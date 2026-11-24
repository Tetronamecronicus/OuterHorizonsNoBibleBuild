using Content.Shared._OuterHorizons.Shuttles;
using Content.Shared.Shuttles.Components;

namespace Content.Client.Shuttles.UI;

// OH-Changes: маршевые двигатели. Нужна кнопка Name="MainEnginesToggle" в NavScreen.xaml.
public sealed partial class NavScreen
{
    [Dependency] private IEntityNetworkManager _net = default!;

    // Последнее значение с сервера, чтобы кнопка не дёргалась между нажатием и ответом.
    private bool _mainEngines;

    private void InitializeMainEngines()
    {
        MainEnginesToggle.OnToggled += args =>
        {
            if (_consoleEntity is { } console)
                _net.SendSystemNetworkMessage(new ShuttleMainEnginesEvent(_entManager.GetNetEntity(console), args.Button.Pressed));
        };
    }

    private void UpdateMainEngines()
    {
        if (_entManager.TryGetComponent(_shuttleEntity, out ShuttleComponent? shuttle) &&
            shuttle.MainEnginesActive != _mainEngines)
        {
            MainEnginesToggle.Pressed = _mainEngines = shuttle.MainEnginesActive;
        }
    }
}
