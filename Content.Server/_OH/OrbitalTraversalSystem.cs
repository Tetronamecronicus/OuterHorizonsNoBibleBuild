using Content.Shared._OH;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._OH;

/// <summary>
/// На сервере правило смены родителя применяется ко всем, кем управляет игрок.
/// </summary>
public sealed class OrbitalTraversalSystem : SharedOrbitalTraversalSystem
{
    // Сущности, которыми управляли на прошлом тике
    private readonly HashSet<EntityUid> _controlled = new();
    private readonly HashSet<EntityUid> _current = new();

    public override void Update(float frameTime)
    {
        _current.Clear();

        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            _current.Add(uid);
            UpdateParent(uid, xform);
        }

        // Тело, которым больше никто не управляет, снова подчиняется обычному поведению движка
        foreach (var uid in _controlled)
        {
            if (_current.Contains(uid))
                continue;

            if (TryComp(uid, out TransformComponent? xform))
                xform.GridTraversal = true;
        }

        _controlled.Clear();
        _controlled.UnionWith(_current);
    }
}
