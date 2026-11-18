using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Shared._OH;
public readonly record struct OrbitElements(double Eccentricity, double Parameter, double PeriapsisAngle, int Direction);
public abstract partial class SharedOrbitalGravitySystem : EntitySystem
{
    [Dependency] protected SharedPhysicsSystem Physics = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;
    [Dependency] protected IGameTiming Timing = default!;
    protected EntityQuery<OrbitalGravityComponent> GravQuery;

    public override void Initialize()
    {
        base.Initialize();

        GravQuery = GetEntityQuery<OrbitalGravityComponent>();
        SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(OnParentChanged);
    }
    private void OnParentChanged(EntityUid uid, PhysicsComponent body, ref EntParentChangedMessage args)
    {
        // При применении состояния с сервера скорость уже пришла готовой
        if (Timing.ApplyingState || body.BodyType != BodyType.Dynamic || args.OldParent is not { } oldParent)
            return;

        var newParent = args.Transform.ParentUid;

        if (!IsGridOrMap(oldParent) || !IsGridOrMap(newParent))
            return;

        var oldParentVel = Physics.GetMapLinearVelocity(oldParent);
        var newParentVel = Physics.GetMapLinearVelocity(newParent);

        Physics.SetLinearVelocity(uid, body.LinearVelocity + oldParentVel - newParentVel, body: body);
    }
    private bool IsGridOrMap(EntityUid uid)
    {
        return HasComp<MapGridComponent>(uid) || HasComp<MapComponent>(uid);
    }

    /// <summary>
    /// Вектор от центра гравитации до центра масс тела.
    /// </summary>
    public Vector2 GetRelative(OrbitalGravityComponent grav, TransformComponent xform, PhysicsComponent body)
    {
        var centerOfMass = Vector2.Transform(body.LocalCenter, Xform.GetWorldMatrix(xform));
        return centerOfMass - grav.Center;
    }

    /// <summary>
    /// Сила притяжения к центру на заданном расстоянии.
    /// </summary>
    public double GetGravityForce(OrbitalGravityComponent grav, double dist, float bodyMass)
    {
        return grav.Mass * (float) grav.G / (dist * dist) * bodyMass;
    }

    /// <summary>
    /// Скорость круговой орбиты на заданном расстоянии.
    /// </summary>
    public float GetCircularSpeed(OrbitalGravityComponent grav, float dist)
    {
        return (float) Math.Sqrt(grav.G * grav.Mass / dist);
    }

    /// <summary>
    /// Считает орбиту по положению и скорости относительно центра притяжения.
    /// Возвращает false, если орбиты нет (тело в центре или не движется).
    /// </summary>
    public static bool TryGetOrbit(OrbitalGravityComponent grav, Vector2 r, Vector2 v, out OrbitElements orbit)
    {
        orbit = default;

        var radius = r.Length();
        double h = r.X * v.Y - r.Y * v.X;

        if (radius < 1f || Math.Abs(h) < 1e-6)
            return false;

        double gm = grav.G * grav.Mass;
        double k = v.LengthSquared() / gm - 1 / radius;
        double radialDot = r.X * v.X + r.Y * v.Y;

        // Вектор эксцентриситета направлен на перицентр.
        double ex = k * r.X - radialDot / gm * v.X;
        double ey = k * r.Y - radialDot / gm * v.Y;

        orbit = new OrbitElements(Math.Sqrt(ex * ex + ey * ey), h * h / gm, Math.Atan2(ey, ex), h > 0 ? 1 : -1);
        return true;
    }
}
