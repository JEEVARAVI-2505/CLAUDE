using System.Windows.Media;
using System.Windows.Media.Media3D;
using BuildingStudio.Engine.Model3D;

namespace BuildingStudio.App.Wpf.Controls;

/// <summary>Converts the engine's <see cref="Scene3D"/> (mm) to a WPF Model3DGroup (metres).</summary>
public static class SceneConverter
{
    public static Model3DGroup ToWpf(Scene3D scene)
    {
        var group = new Model3DGroup();
        foreach (var part in scene.Parts.Where(p => p.TriangleCount > 0).OrderBy(p => p.Opacity < 1))
        {
            var mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection(part.Positions.Select(p => new Point3D(p.X / 1000, p.Y / 1000, p.Z / 1000))),
                Normals = new Vector3DCollection(part.Normals.Select(n => new Vector3D(n.X, n.Y, n.Z))),
                TriangleIndices = new Int32Collection(part.Indices),
            };
            mesh.Freeze();
            var (r, g, b) = part.Rgb();
            var brush = new SolidColorBrush(Color.FromArgb((byte)(part.Opacity * 255), (byte)(r * 255), (byte)(g * 255), (byte)(b * 255)));
            brush.Freeze();
            var material = new DiffuseMaterial(brush);
            group.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
        }
        group.Freeze();
        return group;
    }
}
