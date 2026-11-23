using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared._OuterHorizons.OrbitalGravity;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrbitalBodyComponent : Component
{
    /// <summary>
    /// При инициализации компонента выдаст объекту круговую орбиту.
    /// </summary>
    [DataField] public bool InitCircular = true;

    /// <summary>
    /// OnRails - зафиксирована ли орбита объекта, вместо того чтобы пересчитывать ее каждый тик.
    /// Orbit - данные для фиксации орбиты.
    /// Phase - средняя аномалия.
    /// NextRails - время до следующей фиксации орбиты, если на тело не воздействуют внешние силы.
    /// LastVelocity - требуется для сравнения предсказанной траектории с фактической. Используется
    /// чтобы определить была ли оказана на грид внешняя сила.
    /// </summary>
    [DataField] public bool OnRails;
    [DataField] public OrbitElements Orbit;
    [DataField] public double Phase;
    [DataField] public double NextRails;
    [DataField] public Vector2 LastVelocity;
}
