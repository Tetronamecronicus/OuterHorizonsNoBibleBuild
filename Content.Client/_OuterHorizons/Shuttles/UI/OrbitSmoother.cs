using Content.Shared._OuterHorizons.OrbitalGravity;

namespace Content.Client.Shuttles.UI;

/// Сглаживает орбиту одного грида, чтобы линия не дрожала. Каждое окно держит свой экземпляр.
public sealed class OrbitSmoother
{
    private const double SmoothingTime = 0.1;
    private EntityUid? _gridUid;
    private int _direction;
    private double _ex, _ey, _p;

    public OrbitElements Smooth(EntityUid gridUid, OrbitElements orbit, double frameTime)
    {
        // Сглаживаем вектор эксцентриситета, а не угол: у угла есть разрыв на границе -π/π.
        var ex = orbit.Eccentricity * Math.Cos(orbit.PeriapsisAngle);
        var ey = orbit.Eccentricity * Math.Sin(orbit.PeriapsisAngle);

        // Первый кадр или смена грида/направления: blend = 1, то есть без сглаживания.
        var blend = _gridUid == gridUid && _direction == orbit.Direction
            ? 1 - Math.Exp(-frameTime / SmoothingTime)
            : 1;

        _gridUid = gridUid;
        _direction = orbit.Direction;
        _ex += (ex - _ex) * blend;
        _ey += (ey - _ey) * blend;
        _p += (orbit.Parameter - _p) * blend;

        return new OrbitElements(Math.Sqrt(_ex * _ex + _ey * _ey), _p, Math.Atan2(_ey, _ex), orbit.Direction);
    }
}
