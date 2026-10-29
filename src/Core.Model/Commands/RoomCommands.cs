using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Model.Commands;

/// <summary>Sets the bounds of one or more rooms on a storey (used for swap, resize and shared-wall moves).</summary>
public sealed class SetRoomBoundsCommand(int level, IReadOnlyDictionary<Guid, Rect2> newBounds, string name) : IModelCommand
{
    private Dictionary<Guid, Rect2>? _old;

    public string Name { get; } = name;

    public void Execute(BuildingModel model)
    {
        var storey = model.GetStorey(level) ?? throw new InvalidOperationException($"No storey {level}.");
        _old = new Dictionary<Guid, Rect2>();
        foreach (var (id, rect) in newBounds)
        {
            var room = storey.FindRoom(id) ?? throw new InvalidOperationException($"Room {id} not found.");
            _old[id] = room.Bounds;
            room.Bounds = rect;
        }
    }

    public void Undo(BuildingModel model)
    {
        if (_old is null) return;
        var storey = model.GetStorey(level)!;
        foreach (var (id, rect) in _old) storey.FindRoom(id)!.Bounds = rect;
    }
}

/// <summary>Locks or unlocks a room.</summary>
public sealed class SetRoomLockCommand(Guid roomId, bool locked) : IModelCommand
{
    private bool _old;

    public string Name => locked ? "Lock room" : "Unlock room";

    public void Execute(BuildingModel model)
    {
        var room = model.AllRooms().First(r => r.Id == roomId);
        _old = room.Locked;
        room.Locked = locked;
    }

    public void Undo(BuildingModel model) => model.AllRooms().First(r => r.Id == roomId).Locked = _old;
}

/// <summary>Replaces all storeys (applying a generated option or re-solve result).</summary>
public sealed class ReplaceStoreysCommand(IReadOnlyList<Storey> storeys, string name) : IModelCommand
{
    private List<Storey>? _old;

    public string Name { get; } = name;

    public void Execute(BuildingModel model)
    {
        _old = model.Storeys;
        model.Storeys = storeys.Select(ProjectSerializer.Clone).ToList();
    }

    public void Undo(BuildingModel model)
    {
        if (_old is not null) model.Storeys = _old;
    }
}

/// <summary>Changes the exterior style.</summary>
public sealed class SetExteriorStyleCommand(ExteriorStyle style) : IModelCommand
{
    private ExteriorStyle _old;

    public string Name => $"Style: {style}";

    public void Execute(BuildingModel model)
    {
        _old = model.Brief.Style;
        model.Brief.Style = style;
    }

    public void Undo(BuildingModel model) => model.Brief.Style = _old;
}

/// <summary>Restores a full snapshot (version restore).</summary>
public sealed class RestoreSnapshotCommand(BuildingModel snapshot, string name) : IModelCommand
{
    private string? _old;

    public string Name { get; } = name;

    public void Execute(BuildingModel model)
    {
        _old = ProjectSerializer.Serialize(model);
        Copy(ProjectSerializer.Clone(snapshot), model);
    }

    public void Undo(BuildingModel model)
    {
        if (_old is not null) Copy(ProjectSerializer.Deserialize(_old), model);
    }

    private static void Copy(BuildingModel from, BuildingModel to)
    {
        to.Brief = from.Brief;
        to.Storeys = from.Storeys;
        to.Structure = from.Structure;
        to.Exterior = from.Exterior;
        to.Name = from.Name;
    }
}
