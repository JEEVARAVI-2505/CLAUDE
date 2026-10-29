using BuildingStudio.AI.Services;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Model.Commands;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Model3D;
using BuildingStudio.IO.Gltf;
using BuildingStudio.Studio.Pipeline;

namespace BuildingStudio.Studio.Pipeline.Tests;

public class PipelineTests
{
    private static readonly StudioService Studio = StudioService.Create();

    private static BuildingModel Model(int preset = 1)
    {
        var brief = Studio.Preset(preset);
        return Studio.CreateModel(brief, Studio.Generate(brief, 2)[0]);
    }

    [Fact]
    public void Gate8_one_click_export_produces_full_set()
    {
        var m = Model();
        var dir = Path.Combine(Path.GetTempPath(), $"bs-export-{Guid.NewGuid():N}");
        var result = ExportService.ExportAll(Studio, m, dir);
        foreach (var f in new[] { "project.json", "boq.xlsx", "report.pdf", "model.glb", "viewer.html", "summary.md" })
            new FileInfo(Path.Combine(dir, f)).Length.Should().BeGreaterThan(1000, f);
        result.Files.Count(f => f.EndsWith(".dxf")).Should().BeGreaterThanOrEqualTo(9);
        File.ReadAllBytes(Path.Combine(dir, "report.pdf")).Take(4).Should().Equal("%PDF"u8.ToArray());
        File.ReadAllBytes(Path.Combine(dir, "model.glb")).Take(4).Should().Equal("glTF"u8.ToArray());
        File.ReadAllText(Directory.GetFiles(Path.Combine(dir, "drawings"), "*.dxf")[0]).Should().Contain("SECTION");
        ProjectSerializer.Load(Path.Combine(dir, "project.json")).Storeys.Should().HaveCount(m.Storeys.Count);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Gate4_and_one_model_principle_edit_updates_everything()
    {
        var m = Model();
        var stack = new CommandStack(m);
        var s = m.Storeys[0];
        var bed = s.Rooms.First(r => r.Type is RoomType.Bedroom or RoomType.MasterBedroom);
        var other = s.Rooms.First(r => r.Type == RoomType.Kitchen);
        var before = Studio.Evaluate(m);
        Studio.Swap(stack, 0, bed.Id, other.Id).Should().BeTrue();

        // Walls, furniture and the 3D scene follow the new room positions.
        var kitchen = m.Storeys[0].Rooms.First(r => r.Id == other.Id);
        m.Storeys[0].Furniture.Where(f => f.RoomId == kitchen.Id).Should().OnlyContain(f => kitchen.Bounds.Contains(f.Bounds, 1));
        m.Storeys[0].Walls.Where(w => w.RoomA == kitchen.Id || w.RoomB == kitchen.Id)
            .Should().OnlyContain(w => kitchen.Bounds.Contains(w.Start, 1) && kitchen.Bounds.Contains(w.End, 1));
        SceneBuilder.Build(m).TriangleCount.Should().BeGreaterThan(1000);
        var after = Studio.Evaluate(m);
        after.Vastu.Score.Should().NotBe(before.Vastu.Score);

        Studio.Undo(stack);
        Studio.Evaluate(m).Vastu.Score.Should().Be(before.Vastu.Score);
    }

    [Fact]
    public async Task Gate9_text_edits_work_with_ai_off()
    {
        var m = Model(0);
        var stack = new CommandStack(m);
        (await Studio.ApplyTextEditAsync(stack, "swap kitchen and dining", 0)).Should().StartWith("Swapped");
        (await Studio.ApplyTextEditAsync(stack, "lock living", 0)).Should().Contain("locked");
        m.AllRooms().Single(r => r.Type == RoomType.Living).Locked.Should().BeTrue();
        (await Studio.ApplyTextEditAsync(stack, "do a barrel roll", 0)).Should().StartWith("Sorry");
        (await Studio.BriefAi.ParseAsync("30x40 east facing 2bhk")).Source.Should().Be("Offline rules");
        (await new DesignReviewer(new NoAiProvider()).SummariseAsync(["Vastu score 80/100."])).Should().Contain("80/100");
        var scan = await new PlanImageRecognizerNotAvailable().RecognizeAsync("x.png");
        scan.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Resize_edit_moves_shared_walls()
    {
        var m = Model(0);
        var stack = new CommandStack(m);
        var living = m.Storeys[0].Rooms.Single(r => r.Type == RoomType.Living);
        var msg = await Studio.ApplyTextEditAsync(stack, $"make living {Units.MmToFt(living.Bounds.Width):0.#}x{Units.MmToFt(living.Bounds.Height) - 2:0.#}", 0);
        msg.Should().Contain("Living is now");
        PlanEditor.IsValidTiling(m.Storeys[0]).Should().BeTrue();
    }

    [Fact]
    public void Ai_json_brief_is_schema_checked()
    {
        BriefAssistant.TryRead("""{"floors":1,"site":{"plotWidth":9144,"plotDepth":12192,"facing":"E"},"rooms":[{"type":"Living","floor":0,"count":1,"width":4000,"depth":4500}]}""")
            .Should().NotBeNull();
        BriefAssistant.TryRead("""{"floors":1,"site":{"plotWidth":9144,"plotDepth":12192},"rooms":[{"type":"Living","floor":3,"count":1,"width":4000,"depth":4500}]}""")
            .Should().BeNull("room on a floor that does not exist");
        BriefAssistant.TryRead("not json").Should().BeNull();
    }

    [Fact]
    public void Edit_parser_patterns()
    {
        EditCommandParser.ParseOffline("Move the pooja to north-east")!.Zone.Should().Be(Direction.NE);
        EditCommandParser.ParseOffline("make living 16x14")!.WidthMm.Should().BeApproximately(16 * 304.8, 1e-9);
        EditCommandParser.ParseOffline("swap kitchen with store")!.RoomB.Should().Be("store");
        EditCommandParser.ParseOffline("unlock kitchen")!.Kind.Should().Be(EditKind.Unlock);
    }

    [Fact]
    public void Resolve_and_compare_options()
    {
        var m = Model(0);
        m.Storeys[0].Rooms.First(r => r.Type == RoomType.Kitchen).Locked = true;
        Studio.Resolve(m, 2).Should().NotBeEmpty();
        var brief = Studio.Preset(0);
        var costs = Studio.CompareOptions(brief, Studio.Generate(brief, 2));
        costs.Should().HaveCount(2).And.OnlyContain(c => c.Total > 100000 && c.PerSqft > 500);
    }

    [Fact]
    public void Glb_has_triangles_for_every_style()
    {
        var m = Model(0);
        foreach (var style in Enum.GetValues<ExteriorStyle>())
        {
            m.Brief.Style = style;
            Studio.Rebuild(m);
            GltfExporter.ToGlb(SceneBuilder.Build(m, new SceneOptions(Structure: true))).Length.Should().BeGreaterThan(10000);
            SceneBuilder.Build(m, new SceneOptions(TopLevel: 0)).TriangleCount.Should().BeGreaterThan(100);
        }
    }
}
