using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Systems;

// OH-Changes: маршевые двигатели.
public sealed partial class ThrusterSystem
{
    private static float[] ThrustOf(ShuttleComponent shuttle, ThrusterComponent thruster)
        => thruster.Type == ThrusterType.Main ? shuttle.MainThrust : shuttle.LinearThrust;

    private static List<EntityUid>[] ThrustersOf(ShuttleComponent shuttle, ThrusterComponent thruster)
        => thruster.Type == ThrusterType.Main ? shuttle.MainThrusters : shuttle.LinearThrusters;

    public void SetMainEngines(Entity<ShuttleComponent> shuttle, bool active)
    {
        if (shuttle.Comp.MainEnginesActive == active)
            return;

        shuttle.Comp.MainEnginesActive = active;
        Dirty(shuttle);
        SetMainFiring(shuttle.Comp, active);
    }

    /// <summary>
    /// Включает / выключает факелы и урон у всех маршевых двигателей шаттла.
    /// </summary>
    public void SetMainFiring(ShuttleComponent shuttle, bool firing)
    {
        foreach (var thrusters in shuttle.MainThrusters)
        {
            foreach (var uid in thrusters)
            {
                if (!_thrusterQuery.TryGetComponent(uid, out var comp) || comp.Firing == firing)
                    continue;

                comp.Firing = firing;
                _appearanceQuery.TryGetComponent(uid, out var appearance);
                _appearance.SetData(uid, ThrusterVisualState.Thrusting, firing, appearance);
            }
        }
    }
}
