namespace BuildingStudio.Core.Model.Commands;

/// <summary>A reversible change to the model. All edits go through commands.</summary>
public interface IModelCommand
{
    string Name { get; }
    void Execute(BuildingModel model);
    void Undo(BuildingModel model);
}

/// <summary>Arguments for <see cref="CommandStack.Changed"/>.</summary>
public sealed record ModelChangedEventArgs(string CommandName, bool IsUndo);

/// <summary>Undo/redo stack. Raises <see cref="Changed"/> after every change so engines can recompute.</summary>
public sealed class CommandStack(BuildingModel model)
{
    private readonly Stack<IModelCommand> _undo = new();
    private readonly Stack<IModelCommand> _redo = new();

    public BuildingModel Model { get; private set; } = model;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoName => _undo.Count > 0 ? _undo.Peek().Name : null;

    public event EventHandler<ModelChangedEventArgs>? Changed;

    public void Execute(IModelCommand command)
    {
        command.Execute(Model);
        _undo.Push(command);
        _redo.Clear();
        Changed?.Invoke(this, new ModelChangedEventArgs(command.Name, false));
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var c = _undo.Pop();
        c.Undo(Model);
        _redo.Push(c);
        Changed?.Invoke(this, new ModelChangedEventArgs(c.Name, true));
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var c = _redo.Pop();
        c.Execute(Model);
        _undo.Push(c);
        Changed?.Invoke(this, new ModelChangedEventArgs(c.Name, false));
    }

    /// <summary>Replaces the model (e.g. after loading a file) and clears history.</summary>
    public void Reset(BuildingModel model)
    {
        Model = model;
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke(this, new ModelChangedEventArgs("Load", false));
    }
}
