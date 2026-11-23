using System.Numerics;
using Content.Shared._OuterHorizons.Movement.Events;
using Content.Shared.Movement.Components;

namespace Content.Shared._OuterHorizons.Movement.Systems;

/// <summary>
/// Позволяет игроку с включённым джетпаком корректировать орбиту.
/// </summary>
public sealed partial class OrbitalJetpackSystem : EntitySystem
{
    [Dependency] private EntityQuery<ActiveJetpackComponent> _activeQuery = default!;

    [SubscribeLocalEvent]
    private void OnOrbitalCorrection(Entity<JetpackUserComponent> ent, ref OrbitalCorrectionEvent args)
    {
        args.Impulse = args.WishDir * ent.Comp.WeightlessAcceleration;

        // Топливо тратится только пока игрок жмёт WASD
        if (args.WishDir != Vector2.Zero && _activeQuery.TryComp(ent.Comp.Jetpack, out var active))
            active.Thrusting = true;
    }
}
