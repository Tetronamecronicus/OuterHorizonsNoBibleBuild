using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._OuterHorizons.Shuttles;

[Serializable, NetSerializable]
public sealed class MainEnginesState(bool active) : ComponentState
{
    public bool Active = active;
}

/// <summary>
/// Синхронизирует <see cref="ShuttleComponent.MainEnginesActive"/> с клиентом.
/// </summary>
public sealed class MainEnginesSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ShuttleComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ShuttleComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, ShuttleComponent component, ref ComponentGetState args)
    {
        args.State = new MainEnginesState(component.MainEnginesActive);
    }

    private void OnHandleState(EntityUid uid, ShuttleComponent component, ref ComponentHandleState args)
    {
        if (args.Current is MainEnginesState state)
            component.MainEnginesActive = state.Active;
    }
}
