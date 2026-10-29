using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Geometry.Tests;

public class GeometryTests
{
    [Fact]
    public void Rect_area_center_and_aspect()
    {
        var r = new Rect2(0, 0, 3000, 4500);
        r.Area.Should().Be(13_500_000);
        r.Center.Should().Be(new Vec2(1500, 2250));
        r.Aspect.Should().Be(1.5);
    }

    [Fact]
    public void Touching_rects_share_edge_but_do_not_overlap()
    {
        var a = new Rect2(0, 0, 3000, 4000);
        var b = new Rect2(3000, 1000, 2000, 5000);
        a.Overlaps(b).Should().BeFalse();
        a.TrySharedEdge(b, out var s).Should().BeTrue();
        s.Length.Should().BeApproximately(3000, 1e-9);
        s.A.X.Should().Be(3000);
    }

    [Fact]
    public void Corner_touch_is_not_a_shared_edge()
    {
        new Rect2(0, 0, 1000, 1000).TrySharedEdge(new Rect2(1000, 1000, 1000, 1000), out _).Should().BeFalse();
    }

    [Fact]
    public void Overlap_area()
    {
        new Rect2(0, 0, 2000, 2000).OverlapArea(new Rect2(1000, 1000, 2000, 2000)).Should().Be(1_000_000);
    }

    [Fact]
    public void WithSide_moves_one_edge()
    {
        var r = new Rect2(0, 0, 3000, 3000).WithSide(RectSide.Right, 3600);
        r.Should().Be(new Rect2(0, 0, 3600, 3000));
    }

    [Fact]
    public void Polygon_area_centroid_contains()
    {
        var p = new Polygon([new(0, 0), new(4000, 0), new(4000, 3000), new(0, 3000)]);
        p.Area.Should().Be(12_000_000);
        p.Centroid().Should().Be(new Vec2(2000, 1500));
        p.Contains(new Vec2(1000, 1000)).Should().BeTrue();
        p.Contains(new Vec2(4000, 1000)).Should().BeTrue();
        p.Contains(new Vec2(5000, 1000)).Should().BeFalse();
    }

    [Fact]
    public void L_shaped_polygon_area()
    {
        var p = new Polygon([new(0, 0), new(2000, 0), new(2000, 1000), new(1000, 1000), new(1000, 2000), new(0, 2000)]);
        p.Area.Should().Be(3_000_000);
        p.Contains(new Vec2(1500, 1500)).Should().BeFalse();
        p.Contains(new Vec2(500, 1500)).Should().BeTrue();
    }

    [Fact]
    public void Segment_contains_point()
    {
        var s = new Segment2(new(0, 0), new(1000, 0));
        s.ContainsPoint(new Vec2(500, 0.2)).Should().BeTrue();
        s.ContainsPoint(new Vec2(1200, 0)).Should().BeFalse();
    }

    [Fact]
    public void Rotation_ccw_90()
    {
        var v = new Vec2(1, 0).Rotate(Math.PI / 2);
        v.X.Should().BeApproximately(0, 1e-12);
        v.Y.Should().BeApproximately(1, 1e-12);
    }

    [Theory]
    [InlineData(1000, 150, 1050)]
    [InlineData(1074, 150, 1050)]
    [InlineData(1076, 150, 1050)]
    [InlineData(1125, 150, 1200)]
    public void Snap_to_module(double v, double m, double expected) => Tolerance.Snap(v, m).Should().Be(expected);

    [Fact]
    public void Feet_conversion() => Units.FtToMm(30).Should().BeApproximately(9144, 1e-9);
}
