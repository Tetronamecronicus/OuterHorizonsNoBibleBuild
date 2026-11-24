using Content.Server.Shuttles.Events;
using Content.Shared._OuterHorizons.Shuttles;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

// OuterHorizons: маршевые двигатели.
public sealed partial class ShuttleConsoleSystem
{
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

        if (!TryComp(ev.Console, out TransformComponent? xform) || xform.GridUid is not { } grid)
            return;

        if (msg.Enabled)
            EnsureComp<MainEnginesComponent>(grid);
        else
            RemComp<MainEnginesComponent>(grid);
    }
}
