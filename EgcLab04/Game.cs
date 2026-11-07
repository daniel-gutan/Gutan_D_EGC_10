using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    private readonly Camera _camera = new();

    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R

    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)

    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),
                   Profile = ContextProfile.Core,
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
    }

    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.Resize(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        float dt = (float)args.Time;
        SceneObject sel = _scene[_selected];

        // Shift ținut apăsat → toate mișcările obiectului de 3 ori mai rapide.
        float boost = KeyboardState.IsKeyDown(Keys.LeftShift) ? 3.0f : 1.0f;

        // dt e obligatoriu: fără el, viteza depinde de FPS. La 60 FPS ar fi de 60 de ori
        // mai rapid, iar cu VSync off ar depinde de calculator.
        const float moveSpeed = 2.0f;
        if (KeyboardState.IsKeyDown(Keys.Left)) sel.Transform.Position.X -= moveSpeed * boost * dt;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveSpeed * boost * dt;
        if (KeyboardState.IsKeyDown(Keys.Down)) sel.Transform.Position.Y -= moveSpeed * boost * dt;
        if (KeyboardState.IsKeyDown(Keys.Up)) sel.Transform.Position.Y += moveSpeed * boost * dt;

        // T2: Q/E rotesc 90°/s. Scalarea se face prin înmulțire, nu prin adunare:
        // prin adunare scara ar ajunge la 0 și apoi la valori negative, care oglindesc forma.
        const float rotSpeed = 90.0f;
        if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rotSpeed * boost * dt;
        if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rotSpeed * boost * dt;

        const float scaleRate = 1.5f;
        if (KeyboardState.IsKeyDown(Keys.Z))
            sel.Transform.Scale *= MathF.Pow(1.0f / scaleRate, boost * dt);
        if (KeyboardState.IsKeyDown(Keys.X))
            sel.Transform.Scale *= MathF.Pow(scaleRate, boost * dt);

        float minScale = MathF.Min(sel.Transform.Scale.X, sel.Transform.Scale.Y);
        float maxScale = MathF.Max(sel.Transform.Scale.X, sel.Transform.Scale.Y);
        if (minScale < 0.1f) sel.Transform.Scale *= 0.1f / minScale;
        if (maxScale > 10.0f) sel.Transform.Scale *= 10.0f / maxScale;

        // Aici trebuie IsKeyPressed, nu IsKeyDown: cu IsKeyDown, O s-ar comuta în fiecare
        // cadru cât timp tasta e ținută, deci imaginea ar pâlpâi și rezultatul final ar fi aleator.
        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O)) _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F)) _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R)) ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // T3: clic stânga mută obiectul sub cursor. Depth rămâne neschimbat.
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            sel.Transform.Position = ScreenToWorld(MouseState.Position);
            _titleDirty = true;
        }

        // TODO (T4, facultativ): clic dreapta = pivot; tasta P ținută = rotație în jurul pivotului

        float camSpeed = 3.0f / _camera.Zoom;
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        SetWireframe(_wireframe);
        _shader.Use();

        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // Obiectele se desenează în ordinea din listă, iar ultimul desenat acoperă
        // ce era deja în fereastră, indiferent de adâncime. De aceea în perspectivă
        // un obiect îndepărtat poate apărea peste unul apropiat; se rezolvă cu depth test.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // TODO (T5, facultativ): satelitul obiectului selectat

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // Pasul 1: pixel → NDC. y se întoarce (1 - 2*y/H), pentru că mouse-ul are y în jos,
        // iar NDC are y în sus. Fără asta, clicul ar muta obiectul simetric față de axa orizontală.
        float w = ClientSize.X;
        float h = ClientSize.Y;
        float xNdc = 2.0f * mousePixels.X / w - 1.0f;
        float yNdc = 1.0f - 2.0f * mousePixels.Y / h;

        // Pasul 2: NDC → lume, cu inversa lui ViewMatrix * ProjectionMatrix.
        // O recalculez la fiecare clic, ca să rămână corectă după ce mișc camera sau dau zoom.
        Matrix4 inv = (_camera.ViewMatrix * _camera.ProjectionMatrix).Inverted();
        Vector4 p = new Vector4(xNdc, yNdc, 0.0f, 1.0f) * inv;
        return new Vector2(p.X / p.W, p.Y / p.W);
    }

    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
    }

    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}