using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Model.Commands;

namespace BuildingStudio.Core.Model.Tests;

public class ModelTests
{
    private static BuildingModel Sample()
    {
        var m = new BuildingModel { Name = "Sample" };
        m.Brief.Site.Facing = CardinalSide.E;
        var s = new Storey { Level = 0, Envelope = new Rect2(0, 0, 8000, 10000) };
        s.Rooms.Add(new Room { Type = RoomType.Living, Name = "Living", Bounds = new Rect2(0, 0, 4000, 5000) });
        s.Rooms.Add(new Room { Type = RoomType.Kitchen, Name = "Kitchen", Bounds = new Rect2(4000, 0, 4000, 5000) });
        m.Storeys.Add(s);
        return m;
    }

    [Fact]
    public void Envelope_applies_setbacks_by_facing()
    {
        var site = new Site { PlotWidth = 9000, PlotDepth = 12000, Facing = CardinalSide.E };
        site.Setbacks = new Setbacks { Front = 1500, Rear = 1000, Side = 750 };
        site.Envelope().Should().Be(Rect2.FromMinMax(1000, 750, 7500, 11250));
    }

    [Fact]
    public void Json_round_trip_preserves_model()
    {
        var m = Sample();
        var json = ProjectSerializer.Serialize(m);
        json.Should().Contain("\"type\": \"Living\"").And.NotContain("\"maxX\"");
        var back = ProjectSerializer.Deserialize(json);
        back.Name.Should().Be("Sample");
        back.Storeys[0].Rooms[1].Bounds.Should().Be(new Rect2(4000, 0, 4000, 5000));
        back.Brief.Site.Facing.Should().Be(CardinalSide.E);
    }

    [Fact]
    public void Migrates_v0_title_to_name()
    {
        var back = ProjectSerializer.Deserialize("""{ "title": "Old", "storeys": [] }""");
        back.Name.Should().Be("Old");
        back.Version.Should().Be(BuildingModel.CurrentVersion);
    }

    [Fact]
    public void Rejects_future_version()
    {
        var act = () => ProjectSerializer.Deserialize("""{ "version": 99 }""");
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Save_and_load_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bs-{Guid.NewGuid():N}.json");
        ProjectSerializer.Save(Sample(), path);
        ProjectSerializer.Load(path).Storeys.Should().HaveCount(1);
        File.Delete(path);
    }

    [Fact]
    public void Undo_redo_room_bounds()
    {
        var m = Sample();
        var stack = new CommandStack(m);
        var events = 0;
        stack.Changed += (_, _) => events++;
        var a = m.Storeys[0].Rooms[0];
        var b = m.Storeys[0].Rooms[1];
        stack.Execute(new SetRoomBoundsCommand(0, new Dictionary<Guid, Rect2> { [a.Id] = b.Bounds, [b.Id] = a.Bounds }, "Swap"));
        a.Bounds.X.Should().Be(4000);
        stack.Undo();
        a.Bounds.X.Should().Be(0);
        stack.Redo();
        a.Bounds.X.Should().Be(4000);
        events.Should().Be(3);
        stack.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Lock_and_snapshot_commands_undo()
    {
        var m = Sample();
        var stack = new CommandStack(m);
        var snap = ProjectSerializer.Clone(m);
        var room = m.Storeys[0].Rooms[0];
        stack.Execute(new SetRoomLockCommand(room.Id, true));
        room.Locked.Should().BeTrue();
        stack.Execute(new ReplaceStoreysCommand([], "Clear"));
        m.Storeys.Should().BeEmpty();
        stack.Execute(new RestoreSnapshotCommand(snap, "Restore"));
        m.Storeys.Should().HaveCount(1);
        stack.Undo();
        m.Storeys.Should().BeEmpty();
        stack.Undo();
        stack.Undo();
        m.Storeys[0].Rooms[0].Locked.Should().BeFalse();
    }
}
