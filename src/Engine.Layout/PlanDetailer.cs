using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Layout;

/// <summary>
/// Derives walls, doors and windows from room rectangles. Re-run after every room edit; walls and
/// openings are never edited directly in v1.
/// </summary>
public static class PlanDetailer
{
    public const double ExteriorThickness = 230;
    public const double InteriorThickness = 115;
    public const double DoorHeight = 2100;

    public static void Detail(Storey storey, Core.Model.Brief brief)
    {
        storey.Walls.Clear();
        storey.Openings.Clear();
        BuildWalls(storey);
        AddDoors(storey, brief);
        AddWindows(storey, brief);
    }

    private static void BuildWalls(Storey s)
    {
        for (var i = 0; i < s.Rooms.Count; i++)
        {
            for (var j = i + 1; j < s.Rooms.Count; j++)
            {
                if (!s.Rooms[i].Bounds.TrySharedEdge(s.Rooms[j].Bounds, out var seg)) continue;
                s.Walls.Add(new Wall
                {
                    Start = seg.A, End = seg.B, Thickness = InteriorThickness, Type = WallType.Interior,
                    RoomA = s.Rooms[i].Id, RoomB = s.Rooms[j].Id,
                });
            }
            foreach (var (_, edge) in ModelGeometry.ExteriorEdges(s.Rooms[i].Bounds, s.Envelope))
                s.Walls.Add(new Wall { Start = edge.A, End = edge.B, Thickness = ExteriorThickness, Type = WallType.Exterior, RoomA = s.Rooms[i].Id });
        }
    }

    private static Wall? WallBetween(Storey s, Guid a, Guid b) =>
        s.Walls.FirstOrDefault(w => w.Type == WallType.Interior && ((w.RoomA == a && w.RoomB == b) || (w.RoomA == b && w.RoomB == a)));

    private static void AddDoors(Storey s, Core.Model.Brief brief)
    {
        var used = new HashSet<Guid>();
        foreach (var room in s.Rooms.Where(r => r.Type != RoomType.Passage))
        {
            var target = DoorTarget(s, room);
            if (target is null) continue;
            var wall = WallBetween(s, room.Id, target.Id);
            if (wall is null || !used.Add(wall.Id)) continue;
            var kind = room.Type is RoomType.Staircase || target.Type is RoomType.Staircase ? OpeningKind.Archway : OpeningKind.Door;
            var width = kind == OpeningKind.Archway ? Math.Min(1200, wall.Length - 200)
                : room.Type == RoomType.Bath ? 750 : 900;
            if (width < 600 || wall.Length < width + 100) continue;
            var offset = wall.Length >= width + 300 ? 150 : (wall.Length - width) / 2;
            s.Openings.Add(new Opening { WallId = wall.Id, Offset = offset, Width = width, Height = DoorHeight, Kind = kind });
        }

        if (s.Level == 0) AddMainEntrance(s, brief);
        var front = ModelGeometry.FrontSide(brief.Site);
        foreach (var parking in s.Rooms.Where(r => r.Type == RoomType.Parking))
        {
            var wall = s.Walls.Where(w => w.RoomA == parking.Id && w.Type == WallType.Exterior)
                .OrderByDescending(w => ModelGeometry.EnvelopeSide(s, w) == front).ThenByDescending(w => w.Length).FirstOrDefault();
            if (wall is null || wall.Length < 2700) continue;
            s.Openings.Add(new Opening { WallId = wall.Id, Offset = 300, Width = wall.Length - 600, Height = 2400, Kind = OpeningKind.Archway });
        }
    }

    private static Room? DoorTarget(Storey s, Room room)
    {
        bool Shares(Room o, double min) => o.Id != room.Id && o.Bounds.SharedEdgeLength(room.Bounds) >= min;
        if (room.ParentId is { } pid && s.FindRoom(pid) is { } parent && Shares(parent, 700)) return parent;
        var passage = s.Rooms.Where(o => o.Type == RoomType.Passage && Shares(o, 750))
            .OrderByDescending(o => o.Name.StartsWith("Passage", StringComparison.Ordinal)).ThenByDescending(o => o.Bounds.SharedEdgeLength(room.Bounds)).FirstOrDefault();
        if (passage is not null) return passage;
        if (room.Type == RoomType.Parking) return null;
        if (room.Type is RoomType.Bath or RoomType.Pooja or RoomType.Store)
        {
            var publicRoom = s.Rooms.Where(o => LayoutScorer.IsPublic(o.Type) && Shares(o, 750))
                .OrderByDescending(o => o.Bounds.SharedEdgeLength(room.Bounds)).FirstOrDefault();
            if (publicRoom is not null) return publicRoom;
        }
        int Priority(Room o) => o.Type switch
        {
            RoomType.Living or RoomType.Lounge or RoomType.Dining => 0,
            RoomType.Bedroom or RoomType.MasterBedroom => room.Type is RoomType.Bath or RoomType.Balcony ? 0 : 2,
            RoomType.Kitchen => room.Type is RoomType.Utility or RoomType.Store ? 0 : 2,
            _ => 3,
        };
        return s.Rooms.Where(o => o.Type is not (RoomType.Bath or RoomType.Parking or RoomType.Pooja) && Shares(o, 750))
            .OrderBy(Priority).ThenByDescending(o => o.Bounds.SharedEdgeLength(room.Bounds)).FirstOrDefault();
    }

    private static void AddMainEntrance(Storey s, Core.Model.Brief brief)
    {
        var front = ModelGeometry.FrontSide(brief.Site);
        Wall? FrontWall(RoomType t, bool mainCorridor = false) => s.Walls.Where(w => w.Type == WallType.Exterior
                && s.FindRoom(w.RoomA!.Value) is { } r && r.Type == t && (!mainCorridor || r.Name.StartsWith("Passage", StringComparison.Ordinal))
                && ModelGeometry.EnvelopeSide(s, w) == front && w.Length >= 1650)
            .OrderByDescending(w => w.Length).FirstOrDefault();
        var wall = FrontWall(RoomType.Living) ?? FrontWall(RoomType.Passage, true) ?? FrontWall(RoomType.Dining)
                   ?? FrontWall(RoomType.Lounge) ?? FrontWall(RoomType.Passage)
                   ?? s.Walls.Where(w => w.Type == WallType.Exterior && ModelGeometry.EnvelopeSide(s, w) == front && w.Length >= 1650)
                       .Where(w => s.FindRoom(w.RoomA!.Value)?.Type is not (RoomType.Bath or RoomType.Parking or RoomType.Staircase))
                       .OrderByDescending(w => w.Length).FirstOrDefault();
        if (wall is null) return;

        const double width = 1050;
        // Vastu pada: keep the door toward the north end of an east/west front, or the east end of a north/south front.
        var preferred = brief.Site.Facing is CardinalSide.E or CardinalSide.W ? CardinalSide.N : CardinalSide.E;
        var dir = ModelGeometry.PlanDirectionOf(preferred, brief.Site.NorthAngle);
        var towardEnd = (wall.End - wall.Start).Dot(dir) > 0;
        var offset = wall.Length >= width + 900 ? (towardEnd ? wall.Length - width - 300 : 300) : (wall.Length - width) / 2;
        s.Openings.Add(new Opening { WallId = wall.Id, Offset = offset, Width = width, Height = DoorHeight, Kind = OpeningKind.Door, IsMainEntrance = true });
    }

    private static void AddWindows(Storey s, Core.Model.Brief brief)
    {
        foreach (var room in s.Rooms.Where(r => r.Type is not (RoomType.Passage or RoomType.Parking or RoomType.Balcony)))
        {
            var walls = s.Walls.Where(w => w.Type == WallType.Exterior && w.RoomA == room.Id && w.Length >= 900).ToList();
            var small = room.Type is RoomType.Bath or RoomType.Utility or RoomType.Store or RoomType.Pooja;
            foreach (var wall in walls)
            {
                var (from, to) = FreeSpan(s, wall);
                var len = to - from;
                if (len < 900) continue;
                Opening o;
                if (small)
                    o = new Opening { Width = 600, Height = 450, Sill = 1800, Kind = OpeningKind.Ventilator };
                else if (room.Type == RoomType.Staircase)
                    o = new Opening { Width = Math.Min(900, len - 300), Height = 1200, Sill = 1500, Kind = OpeningKind.Window };
                else
                {
                    var side = ModelGeometry.CompassOf(ModelGeometry.OutwardNormal(s, wall), brief.Site.NorthAngle);
                    var max = side is CardinalSide.N or CardinalSide.E ? 1800 : 1200;
                    o = new Opening { Width = Math.Clamp(len - 600, 600, max), Height = 1200, Sill = 900, Kind = OpeningKind.Window };
                }
                o.WallId = wall.Id;
                o.Offset = from + (len - o.Width) / 2;
                s.Openings.Add(o);
                if (small) break;
            }
        }
    }

    /// <summary>Largest span of a wall not used by an existing opening (with 300 mm margins).</summary>
    private static (double From, double To) FreeSpan(Storey s, Wall wall)
    {
        var taken = s.Openings.Where(o => o.WallId == wall.Id).OrderBy(o => o.Offset).ToList();
        if (taken.Count == 0) return (0, wall.Length);
        var spans = new List<(double, double)>();
        double pos = 0;
        foreach (var o in taken)
        {
            spans.Add((pos, Math.Max(pos, o.Offset - 300)));
            pos = o.Offset + o.Width + 300;
        }
        spans.Add((Math.Min(pos, wall.Length), wall.Length));
        return spans.OrderByDescending(x => x.Item2 - x.Item1).First();
    }
}
