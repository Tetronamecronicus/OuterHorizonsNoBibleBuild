using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

// OH-Changes: маршевые двигатели.
// Атрибуты partial-классов объединяются, поэтому NetworkedComponent объявляем здесь.
// Состояние синхронизируется вручную в MainEnginesSystem (автогенератор не умеет partial-компоненты).
[NetworkedComponent]
public sealed partial class ShuttleComponent
{
    /// <summary>
    /// Пока true, маршевые двигатели постоянно работают, не завися от WASD.
    /// </summary>
    [ViewVariables]
    public bool MainEnginesActive;

    /// <summary>
    /// Аналог <see cref="LinearThrust"/> для маршевых двигателей.
    /// </summary>
    [ViewVariables]
    public readonly float[] MainThrust = new float[4];

    /// <summary>
    /// Аналог <see cref="LinearThrusters"/> для маршевых двигателей.
    /// </summary>
    public readonly List<EntityUid>[] MainThrusters = [[], [], [], []];
}
