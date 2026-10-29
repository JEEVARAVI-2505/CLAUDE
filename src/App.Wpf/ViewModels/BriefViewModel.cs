using System.Collections.ObjectModel;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using BriefModel = BuildingStudio.Core.Model.Brief;

namespace BuildingStudio.App.Wpf.ViewModels;

/// <summary>One editable row of the room table (sizes in feet at the UI edge).</summary>
public partial class RoomRequestViewModel : ObservableObject
{
    [ObservableProperty] private RoomType type;
    [ObservableProperty] private int floor;
    [ObservableProperty] private int count = 1;
    [ObservableProperty] private double widthFt = 10;
    [ObservableProperty] private double depthFt = 10;
    [ObservableProperty] private bool attachedBath;

    public static RoomRequestViewModel From(RoomRequest r) => new()
    {
        Type = r.Type, Floor = r.Floor, Count = r.Count, WidthFt = Math.Round(Units.MmToFt(r.Width), 1),
        DepthFt = Math.Round(Units.MmToFt(r.Depth), 1), AttachedBath = r.AttachedBath,
    };

    public RoomRequest ToModel() => new()
    {
        Type = Type, Floor = Floor, Count = Count, Width = Units.FtToMm(WidthFt), Depth = Units.FtToMm(DepthFt), AttachedBath = AttachedBath,
    };
}

/// <summary>The confirmed brief form. Generation always runs from this form, never from an unconfirmed parse.</summary>
public partial class BriefViewModel : ObservableObject
{
    [ObservableProperty] private string projectName = "New House";
    [ObservableProperty] private double plotWidthFt = 30;
    [ObservableProperty] private double plotDepthFt = 40;
    [ObservableProperty] private CardinalSide facing = CardinalSide.E;
    [ObservableProperty] private double northAngleDeg;
    [ObservableProperty] private int floors = 1;
    [ObservableProperty] private double floorHeightM = 3.0;
    [ObservableProperty] private bool vastu = true;
    [ObservableProperty] private VastuStrictness strictness = VastuStrictness.Moderate;
    [ObservableProperty] private ExteriorStyle style = ExteriorStyle.Contemporary;
    [ObservableProperty] private double frontSetbackM = 1.5;
    [ObservableProperty] private double rearSetbackM = 1.0;
    [ObservableProperty] private double sideSetbackM = 0.75;
    [ObservableProperty] private double soilSbc = 150;
    [ObservableProperty] private int seismicZone = 3;
    [ObservableProperty] private int carParks;

    public ObservableCollection<RoomRequestViewModel> Rooms { get; } = [];

    public static Array Sides => Enum.GetValues<CardinalSide>();
    public static Array RoomTypeValues => Enum.GetValues<RoomType>();
    public static Array StrictnessValues => Enum.GetValues<VastuStrictness>();
    public static Array StyleValues => Enum.GetValues<ExteriorStyle>();

    public void Load(BriefModel b)
    {
        ProjectName = b.ProjectName;
        PlotWidthFt = Math.Round(Units.MmToFt(b.Site.PlotWidth), 1);
        PlotDepthFt = Math.Round(Units.MmToFt(b.Site.PlotDepth), 1);
        Facing = b.Site.Facing;
        NorthAngleDeg = Math.Round(Units.RadToDeg(b.Site.NorthAngle), 1);
        Floors = b.Floors;
        FloorHeightM = b.FloorHeight / 1000;
        Vastu = b.Vastu;
        Strictness = b.Strictness;
        Style = b.Style;
        FrontSetbackM = b.Site.Setbacks.Front / 1000;
        RearSetbackM = b.Site.Setbacks.Rear / 1000;
        SideSetbackM = b.Site.Setbacks.Side / 1000;
        SoilSbc = b.Site.SoilSbc;
        SeismicZone = b.Site.SeismicZone;
        CarParks = b.CarParks;
        Rooms.Clear();
        foreach (var r in b.Rooms) Rooms.Add(RoomRequestViewModel.From(r));
    }

    public BriefModel ToModel() => new()
    {
        ProjectName = ProjectName,
        Floors = Floors,
        FloorHeight = FloorHeightM * 1000,
        Vastu = Vastu,
        Strictness = Strictness,
        Style = Style,
        CarParks = CarParks,
        Site = new Site
        {
            PlotWidth = Units.FtToMm(PlotWidthFt), PlotDepth = Units.FtToMm(PlotDepthFt), Facing = Facing,
            NorthAngle = Units.DegToRad(NorthAngleDeg), SoilSbc = SoilSbc, SeismicZone = SeismicZone,
            Setbacks = new Setbacks { Front = FrontSetbackM * 1000, Rear = RearSetbackM * 1000, Side = SideSetbackM * 1000 },
        },
        Rooms = Rooms.Select(r => r.ToModel()).ToList(),
    };
}
