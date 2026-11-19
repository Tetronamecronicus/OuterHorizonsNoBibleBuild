using Content.Shared._OuterHorizons.OrbitalGravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._OuterHorizons.OrbitalGravity;

/// <summary>
/// На клиенте правило смены родителя применяется только к локальному игроку, и оно то же,
/// что на сервере, поэтому игрок не ждёт решения сервера при переходах.
/// </summary>
public sealed partial class OrbitalTraversalSystem : SharedOrbitalTraversalSystem
{
    [Dependency] private ISharedPlayerManager _player = default!;

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        UpdateParent(player, Transform(player));
    }
}
