using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Camera 2D: matricea view (secțiunea 6) și matricea projection (secțiunea 7).
/// Clasa este completă; o folosiți la exercițiile din clasă și la tema T3.
/// </summary>
public class Camera
{
    public Vector2 Position = Vector2.Zero;   // punctul din lume aflat în centrul imaginii
    public float Zoom = 1.0f;                 // > 1: obiectele apar mai mari; < 1: mai mici
    public float Distance = 5.0f;             // distanța camerei față de planul scenei

    // Cu -Position, camera merge invers decât lumea. Dacă aș pune +Position,
    // scena s-ar mișca în același sens cu tastele, de parcă aș muta lumea, nu camera.
    // Ordinea Translation * Scale face zoom-ul față de punctul de sub cameră;
    // invers, zoom-ul s-ar face față de originea lumii și scena ar fugi spre margine.
    public Matrix4 ViewMatrix =>
        Matrix4.CreateTranslation(-Position.X, -Position.Y, -Distance)
      * Matrix4.CreateScale(Zoom, Zoom, 1.0f);

    public float ViewHeight = 4.0f;
    public float FieldOfView = 45.0f;
    public bool IsPerspective = false;

    public float AspectRatio { get; private set; } = 4.0f / 3.0f;

    // FOV = 45° cadru normal; 90° vezi mai mult (grandangular); 20° vezi puțin, mărit
    // (teleobiectiv); 180° obiectele se reduc la un punct; peste 180° OpenTK aruncă
    // ArgumentOutOfRangeException, pentru că tan(fov/2) devine negativ.
    //
    // Distance = 5: cercul (Depth +2) e la 3 unități, apare normal.
    // Distance = 3: cercul e la 1 unitate, devine uriaș.
    // Distance = 2: cercul e chiar în dreptul camerei, mai aproape decât planul near (0.1),
    // deci e decupat și dispare.
    public Matrix4 ProjectionMatrix => IsPerspective ? Perspective() : Orthographic();

    public void Resize(int width, int height)
    {
        AspectRatio = width / (float)Math.Max(height, 1);
    }

    private Matrix4 Orthographic()
    {
        float h = ViewHeight / 2.0f;
        // w = h * AspectRatio ca cercul să rămână rotund. Cu w = h, zona vizibilă ar fi
        // pătrată într-o fereastră dreptunghiulară, deci cercul ar deveni elipsă (ca în laboratorul 3).
        float w = h * AspectRatio;
        return Matrix4.CreateOrthographicOffCenter(-w, w, -h, h, 0.1f, 100.0f);
    }

    private Matrix4 Perspective() =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), AspectRatio, 0.1f, 100.0f);

    public void Reset()
    {
        Position = Vector2.Zero;
        Zoom = 1.0f;
        IsPerspective = false;
    }
}