using System.Numerics;
using Content.Shared._OuterHorizons.OrbitalGravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server._OuterHorizons.OrbitalGravity;

public sealed class OrbitalGravitySystem : SharedOrbitalGravitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitalBodyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<OrbitalBodyComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.InitCircular)
            return;

        if (!TryComp(ent, out PhysicsComponent? body) || !TryComp(ent, out TransformComponent? xform))
            return;

        if (xform.MapUid is not { } map || !GravQuery.TryComp(map, out var grav))
            return;

        var r = GetRelative(grav, xform, body);
        var dist = r.Length();

        if (dist < 1f || body.Mass <= 0f)
            return;

        var speed = GetCircularSpeed(grav, dist);

        // Перпендикуляр к радиус-вектору, по часовой стрелке
        Physics.SetLinearVelocity(ent, new Vector2(r.Y, -r.X) * (speed / dist), body: body);
    }

    private const float RailsDelay = 2f;      // секунд без внешних сил до перехода на рельсы
    public override void Update(float frameTime)
    {
        if (frameTime <= 0f)
            return;

        var now = Timing.CurTime.TotalSeconds;
        var query = EntityQueryEnumerator<OrbitalBodyComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var orb, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic || body.Mass <= 0f
                || xform.MapUid is not { } map || !GravQuery.TryComp(map, out var grav))
                continue;

            // Скорость изменилась не нами (двигатель, столкновение): слезаем с рельс
            if ((body.LinearVelocity - orb.LastVelocity).Length() > 1e-3f)
            {
                orb.OnRails = false;
                orb.NextRails = now + RailsDelay;
            }

            if (orb.OnRails)
            {
                StepRails(uid, orb, body, xform, grav, now);
            }
            else
            {
                var relative = GetRelative(grav, xform, body);

                if (relative.Length() <= grav.MinRadius)
                {
                    QueueDel(uid);
                    continue;
                }

                var force = GetGravityForce(grav, relative.Length(), body.Mass);

                Physics.ApplyLinearImpulse(uid, -Vector2.Normalize(relative) * (float) force * frameTime, body: body);

                if (now >= orb.NextRails)
                    TryEnterRails(orb, grav, relative, body.LinearVelocity, now);
            }

            orb.LastVelocity = body.LinearVelocity;
        }
    }

    private static double GetMeanMotion(OrbitElements orbit, double gravParameter)
    {
        var semiMajorAxis = orbit.Parameter / (1 - orbit.Eccentricity * orbit.Eccentricity);

        return Math.Sqrt(gravParameter / (semiMajorAxis * semiMajorAxis * semiMajorAxis));
    }

    private static void TryEnterRails(
        OrbitalBodyComponent orb,
        OrbitalGravityComponent grav,
        Vector2 relative,
        Vector2 velocity,
        double now)
    {
        // Только замкнутые орбиты, не задевающие тело
        if (!TryGetOrbit(grav, relative, velocity, out var orbit)
            || orbit.Eccentricity > 0.99
            || orbit.Parameter / (1 + orbit.Eccentricity) <= grav.MinRadius)
            return;

        var trueAnomaly = orbit.Direction * (Math.Atan2(relative.Y, relative.X) - orbit.PeriapsisAngle);
        var eccentricAnomaly = Math.Atan2(
            Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(trueAnomaly),
            orbit.Eccentricity + Math.Cos(trueAnomaly));
        var meanAnomaly = eccentricAnomaly - orbit.Eccentricity * Math.Sin(eccentricAnomaly);

        orb.Orbit = orbit;
        orb.Phase = meanAnomaly - GetMeanMotion(orbit, grav.G * grav.Mass) * now;
        orb.OnRails = true;
    }

    private void StepRails(
        EntityUid uid,
        OrbitalBodyComponent orb,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav,
        double now)
    {
        var gravParameter = grav.G * grav.Mass;

        // Уравнение Кеплера: eccentricAnomaly - e * sin(eccentricAnomaly) = meanAnomaly
        var meanAnomaly = Math.IEEERemainder(orb.Phase + GetMeanMotion(orb.Orbit, gravParameter) * now, Math.Tau);
        var eccentricAnomaly = orb.Orbit.Eccentricity < 0.8 ? meanAnomaly : Math.PI;

        for (var i = 0; i < 10; i++)
        {
            eccentricAnomaly -= (eccentricAnomaly - orb.Orbit.Eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly)
                                / (1 - orb.Orbit.Eccentricity * Math.Cos(eccentricAnomaly));
        }

        var trueAnomaly = Math.Atan2(
            Math.Sqrt(1 - orb.Orbit.Eccentricity * orb.Orbit.Eccentricity) * Math.Sin(eccentricAnomaly),
            Math.Cos(eccentricAnomaly) - orb.Orbit.Eccentricity);
        var angle = orb.Orbit.PeriapsisAngle + orb.Orbit.Direction * trueAnomaly;
        var radial = new Vector2((float) Math.Cos(angle), (float) Math.Sin(angle));
        var tangent = new Vector2(-radial.Y, radial.X) * orb.Orbit.Direction;
        var speedFactor = Math.Sqrt(gravParameter / orb.Orbit.Parameter);

        var distance = orb.Orbit.Parameter / (1 + orb.Orbit.Eccentricity * Math.Cos(trueAnomaly));
        var radialSpeed = speedFactor * orb.Orbit.Eccentricity * Math.Sin(trueAnomaly);
        var tangentSpeed = speedFactor * (1 + orb.Orbit.Eccentricity * Math.Cos(trueAnomaly));

        var centerOfMass = grav.Center + radial * (float) distance;
        var velocity = radial * (float) radialSpeed + tangent * (float) tangentSpeed;

        Physics.SetLinearVelocity(uid, velocity, body: body);
        Xform.SetLocalPosition(uid, centerOfMass - xform.LocalRotation.RotateVec(body.LocalCenter), xform);
    }
}
