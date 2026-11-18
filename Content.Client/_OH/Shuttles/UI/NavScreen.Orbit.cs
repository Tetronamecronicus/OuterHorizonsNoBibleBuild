namespace Content.Client.Shuttles.UI;

public sealed partial class NavScreen
{
    /// Заполняет подписи с данными орбиты своего грида.
    private void UpdateOrbitInfo()
    {
        var orbit = NavRadar.Orbit;
        OrbitApoapsis.Text = orbit?.Apoapsis is { } apoapsis ? $"{apoapsis:0}" : "0.0";
        OrbitPeriapsis.Text = orbit?.Periapsis is { } periapsis ? $"{periapsis:0}" : "0.0";
        OrbitEccentricity.Text = orbit?.Eccentricity is { } eccentricity ? $"{eccentricity:0.000}" : "0.0";
        OrbitPeriod.Text = orbit?.Period is { } period ? $"{period:0} с" : "0.0";
    }
}
