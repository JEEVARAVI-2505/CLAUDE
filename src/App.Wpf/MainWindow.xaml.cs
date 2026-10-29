using System.Windows;
using System.Windows.Input;
using BuildingStudio.App.Wpf.Controls;
using BuildingStudio.App.Wpf.ViewModels;
using BuildingStudio.Engine.Model3D;

namespace BuildingStudio.App.Wpf;

/// <summary>Shell window. Code-behind only wires the custom controls to the view model.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _firstScene = true;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Plan.SwapRequested = vm.RequestSwap;
        Plan.EdgeMoveRequested = vm.RequestEdgeMove;
        Plan.RoomSelected = id =>
        {
            vm.SelectRoom(id);
            Redraw();
        };
        vm.ModelChanged += (_, _) => Redraw();
        InputBindings.Add(new KeyBinding(vm.UndoCommand, Key.Z, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(vm.RedoCommand, Key.Y, ModifierKeys.Control));
        Loaded += (_, _) => vm.LoadPresetCommand.Execute(null);
    }

    private void Redraw()
    {
        var m = _vm.Model;
        Plan.SetData(m, _vm.CurrentLevel, _vm.SelectedRoom?.Id, _vm.ShowZones, _vm.ShowFurniture);
        if (m is null) return;
        var scene = SceneBuilder.Build(m, new SceneOptions(_vm.ShowFurniture, _vm.ShowStructure, true, _vm.FloorCut ? _vm.CurrentLevel : null));
        SceneVisual.Content = SceneConverter.ToWpf(scene);
        if (_firstScene)
        {
            Viewport.ZoomExtents();
            _firstScene = false;
        }
    }

    private void ZoomExtents_Click(object sender, RoutedEventArgs e) => Viewport.ZoomExtents(500);
}
