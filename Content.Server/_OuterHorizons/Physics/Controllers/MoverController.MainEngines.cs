using System.Numerics;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Physics.Controllers;

// OuterHorizons: маршевые двигатели. Постоянная тяга, пока режим включён, независимо от пилота и WASD.
public sealed partial class MoverController
{
    private void HandleMainEngines()
    {
        var query = EntityQueryEnumerator<ShuttleComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shuttle, out var body, out var xform))
        {
            if (!shuttle.MainEnginesActive || !shuttle.Enabled || Paused(uid) || CanPilot(uid))
                continue;

            // Новые / включившиеся двигатели тоже должны получить факел.
            _thruster.SetMainFiring(shuttle, true);

            // Индексы как в HandleShuttleMovement: 0 - South, 1 - East, 2 - North, 3 - West.
            var thrust = shuttle.MainThrust;
            var force = new Vector2(thrust[1] - thrust[3], thrust[2] - thrust[0]);

            if (force != Vector2.Zero)
                PhysicsSystem.ApplyForce(uid, TransformSystem.GetWorldRotation(xform).RotateVec(force), body: body);
        }
    }
}
