using System.Numerics;
using Content.Shared._OuterHorizons.OrbitalGravity;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;

namespace Content.Client._OuterHorizons.OrbitalGravity;

/// <summary>
/// Движок не двигает гриды в предсказании: они стоят на месте до прихода следующего
/// состояния с сервера, а игрок предсказан на несколько тиков вперёд.
/// </summary>
public sealed partial class OrbitalGridPredictionSystem : VirtualController
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    // Гриды дальше этого расстояния от игрока не продвигаем
    private const float Margin = 15f;
    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedMoverController));
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        if (!prediction || _player.LocalEntity is not { } player)
            return;

        var playerXform = Transform(player);

        if (playerXform.ParentUid != playerXform.MapUid)
            return;

        var playerPos = _xform.GetWorldPosition(playerXform);
        var query = EntityQueryEnumerator<OrbitalBodyComponent, MapGridComponent, PhysicsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var grid, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic)
                continue;

            if (body.LinearVelocity == Vector2.Zero && body.AngularVelocity == 0f)
                continue;

            if (xform.MapID != playerXform.MapID)
                continue;

            // Положение игрока в системе координат грида (учитывает поворот грида)
            var localPos = Vector2.Transform(playerPos, _xform.GetInvWorldMatrix(xform));

            if (!grid.LocalAABB.Enlarged(Margin).Contains(localPos))
                continue;

            AdvanceGrid(uid, body, xform, frameTime);
        }
    }

    /// <summary>
    /// Сдвигает и поворачивает центр масс грида, как это делает физика,
    /// после чего вычисляет по нему положение начала координат грида.
    /// </summary>
    private void AdvanceGrid(EntityUid uid, PhysicsComponent body, TransformComponent xform, float frameTime)
    {
        var worldCenter = Vector2.Transform(body.LocalCenter, _xform.GetWorldMatrix(xform));

        var newCenter = worldCenter + body.LinearVelocity * frameTime;
        var newRotation = xform.LocalRotation + new Angle(body.AngularVelocity * frameTime);
        var newPosition = newCenter - newRotation.RotateVec(body.LocalCenter);

        _xform.SetLocalPositionRotation(uid, newPosition, newRotation, xform);
    }
}
