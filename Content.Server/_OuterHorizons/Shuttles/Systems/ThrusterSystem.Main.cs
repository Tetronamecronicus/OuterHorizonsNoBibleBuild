using Content.Server.Shuttles.Components;
using Content.Shared._OuterHorizons.Shuttles;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ThrusterSystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// Постоянная тяга маршевых двигателей, пока на гриде есть <see cref="MainEnginesComponent"/>.
    /// Вызывается из Update.
    /// </summary>
    private void UpdateMainEngines(float frameTime)
    {
        UpdateOverhaul(frameTime);

        var query = EntityQueryEnumerator<ThrusterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Type != ThrusterType.Main)
                continue;

            var grid = xform.GridUid;
            var inFtl = TryComp(grid, out FTLComponent? ftl) &&
                        (ftl.State & (FTLState.Starting | FTLState.Travelling | FTLState.Arriving)) != 0;
            var firing = comp.IsOn && !inFtl && HasComp<MainEnginesComponent>(grid);

            if (comp.Firing != firing)
            {
                comp.Firing = firing;
                _appearance.SetData(uid, ThrusterVisualState.Thrusting, firing);
            }

            if (firing && grid is { } gridUid && TryComp(gridUid, out PhysicsComponent? body))
            {
                // Корабль толкает в сторону, противоположную соплу, то есть по повороту двигателя.
                var thrust = _transform.GetWorldRotation(xform).ToWorldVec() * comp.Thrust * frameTime;
                _physics.ApplyLinearImpulse(gridUid, thrust, body: body);
            }
        }
    }
}
