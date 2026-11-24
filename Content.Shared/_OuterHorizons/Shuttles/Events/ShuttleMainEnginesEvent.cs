using Robust.Shared.Serialization;

namespace Content.Shared._OuterHorizons.Shuttles;

/// <summary>
/// Клиент -> сервер: включить / выключить маршевые двигатели с консоли.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleMainEnginesEvent(NetEntity console, bool enabled) : EntityEventArgs
{
    public NetEntity Console = console;
    public bool Enabled = enabled;
}
