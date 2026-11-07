using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Fabrica de forme: fiecare metodă construiește un obiect Mesh (vârfuri + indici).
/// Un vârf = 6 float: x, y, z, r, g, b (aceeași structură ca în laboratoarele 2 și 3).
/// Formele sunt plate (z = 0) și sunt descrise în object space, cu centrul în origine;
/// locul, mărimea și rotația din scenă sunt date de matricea model (Transform2D).
/// </summary>
public static class Shapes
{
    public static Mesh Triangle()
    {
        float[] vertices =
        {
            -0.5f, -0.5f, 0.0f,   1.0f, 0.0f, 0.0f,
             0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,
             0.0f,  0.5f, 0.0f,   0.0f, 0.0f, 1.0f
        };
        uint[] indices = { 0, 1, 2 };
        return new Mesh(vertices, indices);
    }

    public static Mesh Quad()
    {
        var vertices = new float[4 * Mesh.FloatsPerVertex];
        SetVertex(vertices, 0, -0.5f, -0.5f, 0.0f, 1.0f, 0.0f, 0.0f);
        SetVertex(vertices, 1, 0.5f, -0.5f, 0.0f, 0.0f, 1.0f, 0.0f);
        SetVertex(vertices, 2, 0.5f, 0.5f, 0.0f, 0.0f, 0.0f, 1.0f);
        SetVertex(vertices, 3, -0.5f, 0.5f, 0.0f, 1.0f, 1.0f, 0.0f);
        uint[] indices = { 0, 1, 2, 2, 3, 0 };
        return new Mesh(vertices, indices);
    }

    public static Mesh RegularPolygon(int sides, float radius)
    {
        var vertices = new float[(sides + 1) * Mesh.FloatsPerVertex];
        var indices = new uint[sides * 3];

        SetVertex(vertices, 0, 0.0f, 0.0f, 0.0f, Color4.White);

        for (int i = 0; i < sides; i++)
        {
            float angle = MathF.PI / 2.0f + 2.0f * MathF.PI * i / sides;
            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);
            Color4 color = ColorUtil.FromHsv((float)i / sides, 1.0f, 1.0f);
            SetVertex(vertices, 1 + i, x, y, 0.0f, color);

            indices[3 * i] = 0;
            indices[3 * i + 1] = (uint)(1 + i);
            indices[3 * i + 2] = (uint)(1 + (i + 1) % sides);
        }

        return new Mesh(vertices, indices);
    }

    private static void SetVertex(float[] vertices, int index, float x, float y, float z, float r, float g, float b)
    {
        int i = index * Mesh.FloatsPerVertex;
        vertices[i] = x;
        vertices[i + 1] = y;
        vertices[i + 2] = z;
        vertices[i + 3] = r;
        vertices[i + 4] = g;
        vertices[i + 5] = b;
    }

    private static void SetVertex(float[] vertices, int index, float x, float y, float z, Color4 color)
    {
        SetVertex(vertices, index, x, y, z, color.R, color.G, color.B);
    }
}