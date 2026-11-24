using Content.Server.Shuttles.Events;
using Content.Shared._OuterHorizons.Shuttles;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

// OH-Changes: маршевые двигатели.
public sealed partial class ShuttleConsoleSystem
{
    [Dependency] private ThrusterSystem _thruster = default!;

    private void InitializeMainEngines()
    {
        SubscribeNetworkEvent<ShuttleMainEnginesEvent>(OnMainEnginesRequest);
    }

    private void OnMainEnginesRequest(ShuttleMainEnginesEvent msg, EntitySessionEventArgs args)
    {
        var console = GetEntity(msg.Console);

        // Переключать может только пилот этой консоли.
        if (args.SenderSession.AttachedEntity is not { } pilot ||
            !_pilotQuery.TryGetComponent(pilot, out var pilotComp) ||
            pilotComp.Console != console)
        {
            return;
        }

        // Как в UpdateState: учитывает дрон-консоли.
        var ev = new ConsoleShuttleEvent { Console = console };
        RaiseLocalEvent(console, ref ev);

        if (TryComp(ev.Console, out TransformComponent? xform) &&
            xform.GridUid is { } grid &&
            TryComp(grid, out ShuttleComponent? shuttle))
        {
            _thruster.SetMainEngines((grid, shuttle), msg.Enabled);
        }
    }
}
