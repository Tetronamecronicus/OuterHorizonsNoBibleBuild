using System.Numerics;
using Content.Shared._OH;
using Robust.Client.Graphics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleNavControl
{
    [Dependency] private IGameTiming _timing = default!;

    public readonly record struct OrbitInfo(double Eccentricity, double Periapsis, double? Apoapsis, double? Period);
    public OrbitInfo? Orbit { get; private set; }

    private const int OrbitSegments = 256;
    private const double OrbitMaxRadius = 20000; // гиперболу обрезаем по этому расстоянию

    private readonly Vector2[] _orbitPoints = new Vector2[OrbitSegments + 1];
    private readonly OrbitSmoother _smoother = new();

    private Vector2 GetRelative(OrbitalGravityComponent grav, TransformComponent xform, PhysicsComponent body)
    {
        var com = Vector2.Transform(body.LocalCenter, _transform.GetWorldMatrix(xform));
        return new Vector2(com.X - grav.Center.X, com.Y - grav.Center.Y);
    }

    private void DrawOrbit(DrawingHandleScreen handle, Matrix3x2 worldToView, EntityUid gridUid, Color gridColor, bool own)
    {
        if (!EntManager.TryGetComponent(gridUid, out TransformComponent? xform) ||
            !EntManager.TryGetComponent(gridUid, out PhysicsComponent? body) ||
            !EntManager.HasComponent<OrbitalBodyComponent>(gridUid) ||
            xform.MapUid is not { } map ||
            !EntManager.TryGetComponent(map, out OrbitalGravityComponent? grav) ||
            body.Mass <= 0f)
        {
            return;
        }

        // Положение и скорость относительно центра притяжения (точка та же, что на сервере).
        if (!SharedOrbitalGravitySystem.TryGetOrbit(grav, GetRelative(grav, xform, body), body.LinearVelocity, out var orbit))
            return;

        if (own)
            orbit = _smoother.Smooth(gridUid, orbit, _timing.FrameTime.TotalSeconds);

        var ecc = orbit.Eccentricity;
        var p = orbit.Parameter;
        var periapsisAngle = orbit.PeriapsisAngle;
        var direction = orbit.Direction;

        // Апоапсис и период есть только у замкнутой орбиты.
        var closed = ecc < 1;

        if (own)
        {
            double gm = grav.G * grav.Mass;

            Orbit = new OrbitInfo(
                ecc,
                p / (1 + ecc) - grav.MinRadius,                  // высота перицентра над поверхностью
                closed ? p / (1 - ecc) - grav.MinRadius : null,  // высота апоцентра над поверхностью
                closed ? 2 * Math.PI * Math.Pow(p / (1 - ecc * ecc), 1.5) / Math.Sqrt(gm) : null);
        }

        // Эллипс рисуем целиком, гиперболу (грид улетает) обрезаем по максимальному радиусу.
        var limit = closed ? Math.PI : Math.Acos(Math.Clamp((p / OrbitMaxRadius - 1) / ecc, -1, 1));

        for (var i = 0; i <= OrbitSegments; i++)
        {
            var trueAnomaly = -limit + 2 * limit * i / OrbitSegments;
            _orbitPoints[i] = OrbitPointToView(
                grav.Center,
                periapsisAngle + direction * trueAnomaly,
                p / (1 + ecc * Math.Cos(trueAnomaly)),
                worldToView);
        }

        var color = own ? Color.Cyan.WithAlpha(0.4f) : gridColor.WithAlpha(0.1f);
        handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, _orbitPoints, color);

        // Центр притяжения и апсиды показываем только для своего грида.
        if (!own)
            return;

        // Небесное тело: размер зависит от радиуса, минимум 3 пикселя.
        var centerView = Vector2.Transform(grav.Center, worldToView);
        var bodyRadiusView = Math.Max((float) grav.MinRadius * MinimapScale, 3f);

        handle.DrawCircle(centerView, bodyRadiusView, Color.Cyan.WithAlpha(0.3f));
        handle.DrawCircle(centerView, bodyRadiusView, Color.Cyan, false);

        // У круговой орбиты апсиды не имеют смысла.
        if (ecc < 0.001)
            return;

        DrawApsis(handle, worldToView, grav.Center, periapsisAngle, p / (1 + ecc), "Пе", color, grav.MinRadius);

        if (closed)
            DrawApsis(handle, worldToView, grav.Center, periapsisAngle + Math.PI, p / (1 - ecc), "Ап", color, grav.MinRadius);
    }

    /// Точка на расстоянии distance от центра под углом angle, переведённая в координаты экрана.
    private static Vector2 OrbitPointToView(Vector2 center, double angle, double distance, Matrix3x2 worldToView)
    {
        var offset = new Vector2((float) (distance * Math.Cos(angle)), (float) (distance * Math.Sin(angle)));
        return Vector2.Transform(center + offset, worldToView);
    }

    private void DrawApsis(
        DrawingHandleScreen handle,
        Matrix3x2 worldToView,
        Vector2 center,
        double angle,
        double distance,
        string name,
        Color color,
        double bodyRadius)
    {
        // Точку рисуем по расстоянию от центра, а в подписи показываем высоту над поверхностью.
        var screen = OrbitPointToView(center, angle, distance, worldToView);

        handle.DrawCircle(screen, 4f, color);
        handle.DrawString(Font, screen + new Vector2(8f, -8f), $"{name} {distance - bodyRadius:F0}", color);
    }

    private readonly Vector2[] _gridMarker = new Vector2[3];

    /// Треугольник в центре масс грида, повёрнутый так же, как грид.
    private void DrawGridMarker(DrawingHandleScreen handle, Matrix3x2 worldToView, EntityUid gridUid, Color color)
    {
        if (!EntManager.TryGetComponent(gridUid, out PhysicsComponent? body))
            return;

        var gridToWorld = _transform.GetWorldMatrix(gridUid);
        var center = Vector2.Transform(Vector2.Transform(body.LocalCenter, gridToWorld), worldToView);

        var forward = Vector2.Normalize(Vector2.TransformNormal(Vector2.UnitY, gridToWorld * worldToView));
        var side = new Vector2(-forward.Y, forward.X);

        _gridMarker[0] = center + forward * 12;
        _gridMarker[1] = center - forward * 12 * 0.5f + side * 12 * 0.6f;
        _gridMarker[2] = center - forward * 12 * 0.5f - side * 12 * 0.6f;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _gridMarker, color);
    }

    private void DrawVelocity(DrawingHandleScreen handle, Matrix3x2 worldToView, EntityUid gridUid, Color color)
    {
        if (!EntManager.TryGetComponent(gridUid, out PhysicsComponent? body) ||
            body.LinearVelocity.Length() < 0.1)
            return;

        // Центр масс грида и точка, где он окажется через 6 секунд при текущей скорости.
        var centerWorld = Vector2.Transform(body.LocalCenter, _transform.GetWorldMatrix(gridUid));
        var start = Vector2.Transform(centerWorld, worldToView);
        var end = Vector2.Transform(centerWorld + body.LinearVelocity * 6, worldToView);
        var lineColor = color.WithAlpha(0.5f);

        handle.DrawLine(start, end, lineColor);
        handle.DrawRect(new UIBox2(end.X - 5, end.Y - 5, end.X + 5, end.Y + 5), lineColor);
    }

    // Отладочный след движения грида.
    public const int segmentsA = 5000;
    public List<Vector2> segmentsList = new(new Vector2[segmentsA]);

    private void DrawDebugTrail(DrawingHandleScreen handle, Matrix3x2 worldToView, EntityUid gridUid)
    {
        if (!EntManager.TryGetComponent(gridUid, out TransformComponent? xform) ||
            !EntManager.TryGetComponent(gridUid, out PhysicsComponent? body) ||
            !EntManager.HasComponent<OrbitalBodyComponent>(gridUid) ||
            xform.MapUid is not { } map ||
            !EntManager.TryGetComponent(map, out OrbitalGravityComponent? grav) ||
            body.Mass <= 0f)
        {
            return;
        }

        var color = Color.Red.WithAlpha(0.7f);

        for (var i = 1; i < segmentsList.Count; i++)
        {
            segmentsList[i - 1] = segmentsList[i];
        }

        segmentsList[segmentsList.Count - 1] = xform.WorldPosition;

        for (var i = 1; i < segmentsList.Count; i++)
        {
            handle.DrawLine(Vector2.Transform(segmentsList[i - 1], worldToView), Vector2.Transform(segmentsList[i], worldToView), color);
        }
    }
}
