using Robust.Shared.GameStates;

namespace Content.Shared._OuterHorizons.Shuttles;

/// <summary>
/// Маркер на гриде: пока он есть, маршевые двигатели шаттла работают постоянно.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MainEnginesComponent : Component;
