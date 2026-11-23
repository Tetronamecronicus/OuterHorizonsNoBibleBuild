using System.Numerics;
using Robust.Shared.Physics.Components;
using Content.Shared._OuterHorizons.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    /// <summary>
    /// Движение в невесомости: вместо задания скорости применяем импульс,
    /// чтобы не сбивать орбитальное движение.
    /// </summary>
    private void ApplyOrbitalMovement(EntityUid uid, PhysicsComponent body, Vector2 wishDir, float accel, bool touching)
    {
        var impulse = touching
            ? wishDir * accel - body.LinearVelocity
            : GetOrbitalCorrection(uid, wishDir);

        PhysicsSystem.ApplyLinearImpulse(uid, impulse, body: body);
    }

    /// <summary>
    /// Импульс, которым игрок корректирует свою орбиту в открытом космосе (без джетпака и без опоры).
    /// Сейчас корректировать орбиту нечем, поэтому ввод игнорируется и возвращается ноль.
    /// </summary>
    private Vector2 GetOrbitalCorrection(EntityUid uid, Vector2 wishDir)
    {
        var ev = new OrbitalCorrectionEvent(wishDir);
        RaiseLocalEvent(uid, ref ev);
        return ev.Impulse;
    }
}
