using Content.Shared._OuterHorizons.Movement.Events;
using Content.Shared.Movement.Components;

namespace Content.Shared._OuterHorizons.Movement.Systems;

/// <summary>
/// Позволяет игроку с включённым джетпаком корректировать орбиту.
/// </summary>
public sealed partial class OrbitalJetpackSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnOrbitalCorrection(Entity<JetpackUserComponent> ent, ref OrbitalCorrectionEvent args)
    {
        args.Impulse = args.WishDir * ent.Comp.WeightlessAcceleration;
    }
}
