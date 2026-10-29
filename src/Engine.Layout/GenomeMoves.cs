using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Layout;

/// <summary>Neighbourhood moves for the annealer. Each returns a new genome or null if the move does not apply.</summary>
public static class GenomeMoves
{
    public static Genome? Mutate(Genome source, Random rng)
    {
        var floorsWithRooms = Enumerable.Range(0, source.Floors.Length).Where(f => source.AllSlots(f).Any()).ToList();
        if (floorsWithRooms.Count == 0) return null;
        var g = source.Clone();
        var f = floorsWithRooms[rng.Next(floorsWithRooms.Count)];
        var bands = g.Floors[f];
        var move = rng.Next(10);
        return move switch
        {
            < 4 => SwapSlots(g, bands, rng),
            < 6 => MoveToOtherBand(g, bands, rng),
            < 9 => TogglePair(g, bands, f, rng),
            _ => FlipStack(g, bands, rng),
        };
    }

    private static (int Band, int Index) Pick(List<Slot>[] bands, Random rng)
    {
        var total = bands.Sum(b => b.Count);
        var k = rng.Next(total);
        for (var b = 0; b < bands.Length; b++)
        {
            if (k < bands[b].Count) return (b, k);
            k -= bands[b].Count;
        }
        return (0, 0);
    }

    private static Genome? SwapSlots(Genome g, List<Slot>[] bands, Random rng)
    {
        if (bands.Sum(b => b.Count) < 2) return null;
        var (b1, i1) = Pick(bands, rng);
        var (b2, i2) = Pick(bands, rng);
        if (b1 == b2 && i1 == i2) return null;
        (bands[b1][i1], bands[b2][i2]) = (bands[b2][i2], bands[b1][i1]);
        return g;
    }

    private static Genome? MoveToOtherBand(Genome g, List<Slot>[] bands, Random rng)
    {
        if (bands.Length < 2) return null;
        var (b, i) = Pick(bands, rng);
        if (bands[b].Count < 2) return null;
        var slot = bands[b][i];
        bands[b].RemoveAt(i);
        var other = bands[1 - b];
        other.Insert(rng.Next(other.Count + 1), slot);
        return g;
    }

    private static Genome? TogglePair(Genome g, List<Slot>[] bands, int floor, Random rng)
    {
        var (b, i) = Pick(bands, rng);
        var slot = bands[b][i];
        if (slot.Rooms.Count == 2)
        {
            var real = slot.Rooms.Where(r => r.Type != RoomType.Passage).ToList();
            if (real.Count == 0) return null;
            if (bands[b].Count + real.Count - 1 < 1) return null;
            bands[b].RemoveAt(i);
            bands[b].InsertRange(i, real.Select(r => new Slot([r])));
            return g;
        }
        var room = slot.Rooms[0];
        if (!RoomTypes.IsSmall(room.Type)) return null;

        var partners = bands.SelectMany((band, bi) => band.Select((s, si) => (bi, si, s)))
            .Where(x => x.s != slot && x.s.Rooms.Count == 1 && RoomTypes.IsSmall(x.s.Rooms[0].Type)).ToList();
        if (partners.Count > 0 && rng.Next(2) == 0)
        {
            var (pb, pi, ps) = partners[rng.Next(partners.Count)];
            if (bands[pb].Count < 2 && pb != b) return null;
            slot.Rooms.Add(ps.Rooms[0]);
            bands[pb].RemoveAt(pi);
            return g;
        }
        // Stack with a lobby/dress filler on the corridor side so the small room gets sensible proportions.
        slot.Rooms.Add(new RoomSpec($"{floor}:Lobby:{room.Key}", RoomType.Passage, "Lobby", room.TargetArea * 0.6, floor, null));
        return g;
    }

    private static Genome? FlipStack(Genome g, List<Slot>[] bands, Random rng)
    {
        var stacked = bands.SelectMany(b => b).Where(s => s.Rooms.Count == 2).ToList();
        if (stacked.Count == 0) return null;
        var s = stacked[rng.Next(stacked.Count)];
        s.Rooms.Reverse();
        return g;
    }
}
