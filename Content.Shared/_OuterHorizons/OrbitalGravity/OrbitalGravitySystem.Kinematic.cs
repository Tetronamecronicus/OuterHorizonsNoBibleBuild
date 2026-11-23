using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;

namespace Content.Shared._OuterHorizons.OrbitalGravity;

/// <summary>
/// Гравитация для кинематических тел в открытом космосе.
/// </summary>
public sealed class OrbitalKinematicGravitySystem : VirtualController
{
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<OrbitalGravityComponent> _gravQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedMoverController)); // Гравитация после мувера, чтобы он не затёр её своей скоростью
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<OrbitalBodyComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var body, out var xform))
        {
            if (body.BodyType is not (BodyType.Kinematic or BodyType.KinematicController))
                continue;

            if (_net.IsClient && !(prediction && body.Predict))
                continue;

            if (xform.MapUid is not { } map || xform.ParentUid != map || !_gravQuery.TryComp(map, out var grav))
                continue;

            var relative = _xform.GetWorldPosition(xform) - grav.Center;
            var dist = relative.Length();

            if (dist <= grav.MinRadius)
            {
                if (_net.IsServer)
                    QueueDel(uid);

                continue;
            }

            var acceleration = grav.G * grav.Mass / (dist * dist);

            PhysicsSystem.SetLinearVelocity(
                uid,
                body.LinearVelocity - relative / dist * (float)acceleration * frameTime,
                body: body);
        }
    }
}
