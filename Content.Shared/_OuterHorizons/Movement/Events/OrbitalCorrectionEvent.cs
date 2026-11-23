using System.Numerics;

namespace Content.Shared._OuterHorizons.Movement.Events;

/// <summary>
/// Спрашивает у сущности, какой импульс она может приложить для коррекции своей орбиты.
/// Подписчик (например, джетпак) записывает результат в Impulse.
/// </summary>
[ByRefEvent]
public record struct OrbitalCorrectionEvent(Vector2 WishDir, Vector2 Impulse = default);
