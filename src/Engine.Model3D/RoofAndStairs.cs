using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Model3D;

/// <summary>Parapet, sloped tile roof, pergola and stair headroom (mumty).</summary>
public static class Roof
{
    public static void Build(Scene3D scene, BuildingModel model)
    {
        var ext = model.Exterior;
        var env = model.Storeys[0].Envelope.Inflate(115);
        var z = model.RoofLevel();
        var wall = scene.Part("Walls", ext.WallColor);
        var trim = scene.Part("Roof", ext.TrimColor);

        var stair = model.Storeys[^1].Rooms.FirstOrDefault(r => r.Type == RoomType.Staircase);
        if (stair is not null && model.Storeys.Count > 1 && ext.Roof != RoofForm.SlopedTile)
        {
            wall.AddBox(stair.Bounds, z, z + 2700);
            trim.AddBox(stair.Bounds.Inflate(300), z + 2700, z + 2850);
        }

        if (ext.Roof == RoofForm.SlopedTile)
        {
            SlopedRoof(scene.Part("Roof", ext.AccentColor), env.Inflate(750), z, 25);
            return;
        }
        const double t = 115;
        var h = ext.ParapetHeight;
        wall.AddBox(Rect2.FromMinMax(env.MinX, env.MinY, env.MaxX, env.MinY + t), z, z + h);
        wall.AddBox(Rect2.FromMinMax(env.MinX, env.MaxY - t, env.MaxX, env.MaxY), z, z + h);
        wall.AddBox(Rect2.FromMinMax(env.MinX, env.MinY + t, env.MinX + t, env.MaxY - t), z, z + h);
        wall.AddBox(Rect2.FromMinMax(env.MaxX - t, env.MinY + t, env.MaxX, env.MaxY - t), z, z + h);
        trim.AddBox(Rect2.FromMinMax(env.MinX - 40, env.MinY - 40, env.MaxX + 40, env.MinY + t), z + h - 100, z + h);
        trim.AddBox(Rect2.FromMinMax(env.MinX - 40, env.MaxY - t, env.MaxX + 40, env.MaxY + 40), z + h - 100, z + h);
        if (ext.CorniceBand)
            foreach (var s in model.Storeys)
                trim.AddBox(env.Inflate(60), s.Elevation + s.Height - 150, s.Elevation + s.Height);

        if (ext.Roof == RoofForm.Pergola)
        {
            var p = scene.Part("Roof", ext.AccentColor);
            var area = Rect2.FromMinMax(env.Center.X, env.MinY + 300, env.MaxX - 300, env.Center.Y);
            foreach (var corner in area.Corners()) p.AddBox(new Rect2(corner.X - 100, corner.Y - 100, 200, 200), z, z + 2400);
            for (var x = area.MinX; x <= area.MaxX; x += 450) p.AddBox(new Rect2(x, area.MinY - 150, 75, area.Height + 300), z + 2400, z + 2600);
        }
    }

    /// <summary>Gable roof with the ridge along the long side.</summary>
    private static void SlopedRoof(MeshPart part, Rect2 r, double z, double pitchDeg)
    {
        var alongX = r.Width >= r.Height;
        var span = alongX ? r.Height : r.Width;
        var rise = span / 2 * Math.Tan(pitchDeg * Math.PI / 180);
        Vec3 P(double x, double y, double h) => Vec3.FromPlan(new Vec2(x, y), z + h);
        if (alongX)
        {
            var cy = r.Center.Y;
            part.AddQuad(P(r.MinX, r.MinY, 0), P(r.MaxX, r.MinY, 0), P(r.MaxX, cy, rise), P(r.MinX, cy, rise));
            part.AddQuad(P(r.MaxX, r.MaxY, 0), P(r.MinX, r.MaxY, 0), P(r.MinX, cy, rise), P(r.MaxX, cy, rise));
            part.AddTriangle(P(r.MinX, r.MaxY, 0), P(r.MinX, r.MinY, 0), P(r.MinX, cy, rise));
            part.AddTriangle(P(r.MaxX, r.MinY, 0), P(r.MaxX, r.MaxY, 0), P(r.MaxX, cy, rise));
        }
        else
        {
            var cx = r.Center.X;
            part.AddQuad(P(r.MinX, r.MaxY, 0), P(r.MinX, r.MinY, 0), P(cx, r.MinY, rise), P(cx, r.MaxY, rise));
            part.AddQuad(P(r.MaxX, r.MinY, 0), P(r.MaxX, r.MaxY, 0), P(cx, r.MaxY, rise), P(cx, r.MinY, rise));
            part.AddTriangle(P(r.MinX, r.MinY, 0), P(r.MaxX, r.MinY, 0), P(cx, r.MinY, rise));
            part.AddTriangle(P(r.MaxX, r.MaxY, 0), P(r.MinX, r.MaxY, 0), P(cx, r.MaxY, rise));
        }
    }
}

/// <summary>Dog-legged stair: two flights along the long side with a landing at the far end.</summary>
public static class Stairs
{
    public static void Build(Scene3D scene, Rect2 room, double elevation, double height)
    {
        var part = scene.Part("Stairs", "#8d8d8d");
        var alongY = room.Height >= room.Width;
        var len = alongY ? room.Height : room.Width;
        var wid = alongY ? room.Width : room.Height;
        var flight = (wid - 100) / 2;
        var risers = (int)Math.Ceiling(height / 190 - 1e-9);
        if (risers % 2 == 1) risers++;
        var riser = height / risers;
        var perFlight = risers / 2;
        var landing = flight;
        var tread = (len - landing) / Math.Max(1, perFlight - 1);

        Rect2 R(double l0, double l1, double w0, double w1) => alongY
            ? Rect2.FromMinMax(room.MinX + w0, room.MinY + l0, room.MinX + w1, room.MinY + l1)
            : Rect2.FromMinMax(room.MinX + l0, room.MinY + w0, room.MinX + l1, room.MinY + w1);

        for (var i = 0; i < perFlight - 1; i++)
            part.AddBox(R(i * tread, (i + 1) * tread, 0, flight), elevation, elevation + (i + 1) * riser);
        var mid = elevation + perFlight * riser;
        part.AddBox(R(len - landing, len, 0, wid), mid - 150, mid);
        for (var i = 0; i < perFlight - 1; i++)
        {
            var l1 = len - landing - i * tread;
            part.AddBox(R(l1 - tread, l1, wid - flight, wid), mid, mid + (i + 1) * riser);
        }
    }
}
