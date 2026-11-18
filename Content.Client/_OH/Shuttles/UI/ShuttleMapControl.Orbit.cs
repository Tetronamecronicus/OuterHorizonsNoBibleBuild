using System.Numerics;
using Content.Shared._OH;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.UI.MapObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map.Components;

namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleMapControl
{
    private const int OrbitSegments = 128;
    private const double OrbitMaxRadius = 20000; // гиперболу обрезаем по этому расстоянию

    private readonly Vector2[] _orbitPoints = new Vector2[OrbitSegments + 1];
    private readonly OrbitSmoother _smoother = new();

    /// Вызывается из конструктора: орбиты большие, поэтому разрешаем сильное отдаление.
    private void InitOrbits()
    {
        WorldMaxRange = 50000f;
    }

    /// Небесное тело и орбиты рисуются под гридами и подписями.
    private void DrawOrbitOverlay(DrawingHandleScreen handle, Matrix3x2 matty, List<IMapObject> mapObjects, EntityUid mapUid)
    {
        DrawOrbitalBody(handle, matty, mapUid);
        DrawOrbits(handle, matty, mapObjects, mapUid);
    }

    /// Мировая точка в координаты экрана (так же считается положение гридов и маяков).
    private Vector2 WorldToView(Matrix3x2 matty, Vector2 worldPos)
    {
        var pos = Vector2.Transform(worldPos, matty);
        return ScalePosition(pos with { Y = -pos.Y });
    }

    /// Небесное тело: окружность радиуса MinRadius.
    private void DrawOrbitalBody(DrawingHandleScreen handle, Matrix3x2 matty, EntityUid mapUid)
    {
        if (!EntManager.TryGetComponent(mapUid, out OrbitalGravityComponent? grav))
            return;

        var centerView = WorldToView(matty, grav.Center);

        // Радиус в пикселях, минимум 3, чтобы тело не пропадало при отдалении.
        var radiusView = Math.Max((float) grav.MinRadius * MinimapScale, 3f);

        handle.DrawCircle(centerView, radiusView, Color.Cyan.WithAlpha(0.3f));
        handle.DrawCircle(centerView, radiusView, Color.Cyan, filled: false);
    }

    /// Траектории орбит всех гридов карты.
    private void DrawOrbits(DrawingHandleScreen handle, Matrix3x2 matty, List<IMapObject> mapObjects, EntityUid mapUid)
    {
        if (!EntManager.TryGetComponent(mapUid, out OrbitalGravityComponent? grav))
            return;

        // Идём по всем объектам, а не только по видимым: орбита может быть на экране, когда сам грид за его пределами.
        foreach (var mapObj in mapObjects)
        {
            if (mapObj is not GridMapObject gridObj ||
                EntManager.HasComponent<MapComponent>(gridObj.Entity) ||
                !EntManager.HasComponent<OrbitalBodyComponent>(gridObj.Entity) ||
                !EntManager.TryGetComponent(gridObj.Entity, out MapGridComponent? mapGrid) ||
                !_physicsQuery.TryGetComponent(gridObj.Entity, out var gridPhysics))
            {
                continue;
            }

            Entity<MapGridComponent> grid = (gridObj.Entity, mapGrid);

            // Положение центра масс грида, как и на сервере.
            var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(grid.Owner);
            gridPos = Maps.GetGridPosition((grid, gridPhysics), gridPos, gridRot);

            if (!SharedOrbitalGravitySystem.TryGetOrbit(grav, gridPos - grav.Center, gridPhysics.LinearVelocity, out var orbit))
                continue;

            var own = grid.Owner == _shuttleEntity;

            // Сглаживаем только орбиту своего грида.
            if (own)
                orbit = _smoother.Smooth(grid.Owner, orbit, _timing.FrameTime.TotalSeconds);

            // Эллипс рисуем целиком, гиперболу (грид улетает) обрезаем по максимальному радиусу.
            var ecc = orbit.Eccentricity;
            var limit = ecc < 1
                ? Math.PI
                : Math.Acos(Math.Clamp((orbit.Parameter / OrbitMaxRadius - 1) / ecc, -1, 1));

            for (var i = 0; i <= OrbitSegments; i++)
            {
                var trueAnomaly = -limit + 2 * limit * i / OrbitSegments;
                var angle = orbit.PeriapsisAngle + orbit.Direction * trueAnomaly;
                var distance = orbit.Parameter / (1 + ecc * Math.Cos(trueAnomaly));

                var offset = new Vector2((float) (distance * Math.Cos(angle)), (float) (distance * Math.Sin(angle)));
                _orbitPoints[i] = WorldToView(matty, grav.Center + offset);
            }

            EntManager.TryGetComponent(grid, out IFFComponent? iffComp);
            var color = _shuttles.GetIFFColor(grid, self: own, component: iffComp);

            // Свой грид: линия 0.6, апсиды непрозрачные. Чужие: всё на 80% прозрачное.
            var lineColor = color.WithAlpha(own ? 0.6f : 0.2f);
            var apsisColor = own ? color : color.WithAlpha(0.2f);

            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, _orbitPoints, lineColor);

            // У круговой орбиты апсиды не имеют смысла.
            if (ecc < 0.001)
                continue;

            DrawApsis(handle, matty, grav, orbit.PeriapsisAngle, orbit.Parameter / (1 + ecc), "Пе", apsisColor);

            // Апоцентр есть только у замкнутой орбиты.
            if (ecc < 1)
                DrawApsis(handle, matty, grav, orbit.PeriapsisAngle + Math.PI, orbit.Parameter / (1 - ecc), "Ап", apsisColor);
        }
    }

    /// Точка апсиды на орбите и подпись с высотой над поверхностью небесного тела.
    private void DrawApsis(
        DrawingHandleScreen handle,
        Matrix3x2 matty,
        OrbitalGravityComponent grav,
        double angle,
        double distance,
        string name,
        Color color)
    {
        var offset = new Vector2((float) (distance * Math.Cos(angle)), (float) (distance * Math.Sin(angle)));
        var screen = WorldToView(matty, grav.Center + offset);

        handle.DrawCircle(screen, 4f, color);
        handle.DrawString(_font, screen + new Vector2(8f, -8f), $"{name} {distance - grav.MinRadius:0}", color);
    }
}
