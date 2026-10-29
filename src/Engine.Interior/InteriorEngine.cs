using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Engine.Interior;

/// <summary>Electrical points for one room (lux rule for lights).</summary>
public sealed record ElectricalRow(string Room, int Level, double AreaSqm, int Lights, int Fans, int Sockets, int PowerSockets)
{
    public int Total => Lights + Fans + Sockets + PowerSockets;
}

/// <summary>Auto-furnish, finishes and the electrical point schedule.</summary>
public sealed class InteriorEngine(FurnitureCatalog catalog)
{
    public FurnitureCatalog Catalog { get; } = catalog;

    /// <summary>Furnishes every room of every storey. <paramref name="variant"/> picks alternatives ("bed-king|bed-queen").</summary>
    public void Furnish(BuildingModel model, int variant = 0)
    {
        foreach (var s in model.Storeys)
        {
            s.Furniture.Clear();
            foreach (var r in s.Rooms) s.Furniture.AddRange(FurnishRoom(s, r, model.Site.NorthAngle, variant));
        }
    }

    public List<FurnitureItem> FurnishRoom(Storey storey, Room room, double northAngle, int variant = 0)
    {
        if (!Catalog.Templates.TryGetValue(room.Type, out var template)) return [];
        var placer = new FurniturePlacer(storey, room, northAngle);
        placer.Prepare();
        foreach (var slot in template)
        {
            var options = slot.Split('|');
            for (var k = 0; k < options.Length; k++)
            {
                var item = Catalog.Find(options[(variant + k) % options.Length]);
                if (item is not null && placer.TryPlace(item)) break;
            }
        }
        return placer.Placed;
    }

    public void ApplyFinishes(BuildingModel model)
    {
        foreach (var r in model.AllRooms())
            if (Catalog.Finishes.TryGetValue(r.Type, out var f))
            {
                r.FloorFinish = f.Floor;
                r.WallColor = f.Wall;
            }
    }

    /// <summary>
    /// Lights = ceil(A × E / (Φ × UF·MF)) (lumen method), fans = 1 per 12 m² in habitable rooms,
    /// 5 A sockets per finish table, 15 A power sockets for kitchen/utility/bath.
    /// </summary>
    public IReadOnlyList<ElectricalRow> Electrical(BuildingModel model)
    {
        var rows = new List<ElectricalRow>();
        foreach (var s in model.Storeys)
            foreach (var r in s.Rooms)
            {
                var area = r.Area / 1e6;
                var finish = Catalog.Finishes.GetValueOrDefault(r.Type) ?? new RoomFinish();
                var lights = Math.Max(1, (int)Math.Ceiling(area * finish.Lux / (Catalog.FixtureLumens * Catalog.UtilisationTimesMaintenance) - 1e-9));
                var fans = RoomTypes.IsHabitable(r.Type) && r.Type != RoomType.Kitchen ? Math.Max(1, (int)Math.Round(area / 12)) : 0;
                var sockets = (int)Math.Ceiling(area / 10 * finish.SocketsPer10Sqm - 1e-9);
                var power = r.Type switch { RoomType.Kitchen => 3, RoomType.Utility => 1, RoomType.Bath => 1, _ => 0 };
                rows.Add(new ElectricalRow(r.Name, s.Level, Math.Round(area, 2), lights, fans, sockets, power));
            }
        return rows;
    }
}
