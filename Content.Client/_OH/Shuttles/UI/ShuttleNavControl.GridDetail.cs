using System.Numerics;

namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleNavControl
{
    // Подбери значения под свой радар: по умолчанию масштаб радара меняется от 64 до 256 тайлов.
    private const float DetailMaxRange = 200f;     // при большем отдалении внешний вид чужих гридов не рисуем
    private const float DetailMaxDistance = 256f;  // чужие гриды дальше этого расстояния от радара тоже рисуем без внешнего вида

    /// Нужно ли рисовать внешний вид (корпус и стыковочные порты) чужого грида.
    private bool ShouldDrawGridDetail(Vector2 gridCenterWorld, Vector2 radarWorldPos)
    {
        // Камера слишком далеко.
        if (WorldRange > DetailMaxRange)
            return false;

        // Грид слишком далеко от радара.
        return (gridCenterWorld - radarWorldPos).LengthSquared() <= DetailMaxDistance * DetailMaxDistance;
    }
}
