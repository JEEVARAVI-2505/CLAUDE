using System.Numerics;
using BuildingStudio.Engine.Model3D;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

namespace BuildingStudio.IO.Gltf;

using VERTEX = VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>;

/// <summary>Writes a <see cref="Scene3D"/> as binary glTF (GLB). Converts millimetres to metres at this edge.</summary>
public static class GltfExporter
{
    public static byte[] ToGlb(Scene3D scene)
    {
        var builder = new SharpGLTF.Scenes.SceneBuilder();
        foreach (var part in scene.Parts.Where(p => p.TriangleCount > 0))
        {
            var (r, g, b) = part.Rgb();
            var material = new MaterialBuilder($"{part.Group}-{part.Color}")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(r, g, b, (float)part.Opacity))
                .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, 0f)
                .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, 0.85f);
            if (part.Opacity < 1) material.WithAlpha(AlphaMode.BLEND);

            var mesh = new MeshBuilder<VertexPositionNormal>(part.Group);
            var prim = mesh.UsePrimitive(material);
            VERTEX V(int i)
            {
                var p = part.Positions[i];
                var n = part.Normals[i];
                return new VERTEX(new VertexPositionNormal(
                    new Vector3((float)(p.X / 1000), (float)(p.Y / 1000), (float)(p.Z / 1000)),
                    new Vector3((float)n.X, (float)n.Y, (float)n.Z)));
            }
            for (var t = 0; t < part.Indices.Count; t += 3)
                prim.AddTriangle(V(part.Indices[t]), V(part.Indices[t + 1]), V(part.Indices[t + 2]));
            builder.AddRigidMesh(mesh, Matrix4x4.Identity);
        }
        var model = builder.ToGltf2();
        return model.WriteGLB().ToArray();
    }

    public static void Save(Scene3D scene, string path) => File.WriteAllBytes(path, ToGlb(scene));
}
