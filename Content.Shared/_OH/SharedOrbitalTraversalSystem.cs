using System.Numerics;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._OH;

/// <summary>
/// Смена родителя игрока на орбитальных картах. Штатный переход между гридами здесь
/// отключён: он срабатывает ровно на границе грида, где ошибка в сантиметры даёт
/// разные решения на клиенте и сервере. Вместо него работает гистерезис: игрок остаётся
/// ребёнком грида, пока не отлетел от него на <see cref="DetachDistance"/>, и цепляется
/// за грид, только когда подошёл на <see cref="AttachDistance"/>. Пока под игроком
/// нет тайла родителя-грида, он считается невесомым.
/// </summary>
public abstract partial class SharedOrbitalTraversalSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;

    private const float AttachDistance = 1f;
    private const float DetachDistance = 2f;

    private EntityQuery<OrbitalGravityComponent> _gravQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<GravityAffectedComponent> _affectedQuery;

    public override void Initialize()
    {
        base.Initialize();

        _gravQuery = GetEntityQuery<OrbitalGravityComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _affectedQuery = GetEntityQuery<GravityAffectedComponent>();

        SubscribeLocalEvent<GravityAffectedComponent, IsWeightlessEvent>(OnIsWeightless);

        // Родитель меняется после физики, в одном порядке на клиенте и сервере
        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    protected void UpdateParent(EntityUid uid, TransformComponent xform)
    {
        if (xform.MapUid is not { } map)
            return;

        if (!_gravQuery.HasComp(map))
        {
            xform.GridTraversal = true;
            return;
        }

        if (xform.ParentUid != map && !_gridQuery.HasComp(xform.ParentUid))
            return;

        // Родителя меняем сами, штатный переход между гридами отключаем
        xform.GridTraversal = false;

        var target = ChooseParent(xform, map);

        if (target != xform.ParentUid)
            _xform.SetParent(uid, xform, target);

        RefreshGravityState(uid, xform);
    }

    private EntityUid ChooseParent(TransformComponent xform, EntityUid map)
    {
        if (_gridQuery.TryComp(xform.ParentUid, out var parentGrid)
            && IsOverTile(xform.ParentUid, parentGrid, xform.LocalPosition))
        {
            return xform.ParentUid;
        }

        var worldPos = _xform.GetWorldPosition(xform);
        var currentDist = float.PositiveInfinity;
        var other = EntityUid.Invalid;
        var otherDist = float.PositiveInfinity;

        var grids = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (grids.MoveNext(out var gridUid, out var grid, out var gridXform))
        {
            if (gridXform.MapUid != map)
                continue;

            var dist = GetDistanceToGrid(gridUid, grid, gridXform, worldPos);

            if (gridUid == xform.ParentUid)
            {
                currentDist = dist;
            }
            else if (dist < otherDist)
            {
                other = gridUid;
                otherDist = dist;
            }
        }

        if (otherDist == 0f)
            return other;

        if (currentDist <= AttachDistance)
            return xform.ParentUid;

        if (otherDist <= AttachDistance)
            return other;

        return currentDist <= DetachDistance ? xform.ParentUid : map;
    }

    /// <summary>
    /// Расстояние от точки в мире до ближайшего тайла грида. Ноль, если точка над тайлом.
    /// Дальше <see cref="DetachDistance"/> точное значение не нужно, там оно может быть любым большим.
    /// </summary>
    private float GetDistanceToGrid(EntityUid gridUid, MapGridComponent grid, TransformComponent gridXform, Vector2 worldPos)
    {
        var localPos = Vector2.Transform(worldPos, _xform.GetInvWorldMatrix(gridXform));

        if (!grid.LocalAABB.Enlarged(DetachDistance).Contains(localPos))
            return float.PositiveInfinity;

        float size = grid.TileSize;
        var center = GetTile(grid, localPos);
        var radius = (int)MathF.Ceiling(DetachDistance / size);
        var best = float.PositiveInfinity;

        for (var x = center.X - radius; x <= center.X + radius; x++)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
            {
                if (!HasTile(gridUid, grid, new Vector2i(x, y)))
                    continue;

                // Ближайшая к игроку точка этого тайла
                var min = new Vector2(x, y) * size;
                var closest = Vector2.Clamp(localPos, min, min + new Vector2(size));
                var dist = Vector2.Distance(localPos, closest);

                if (dist == 0f)
                    return 0f;

                best = MathF.Min(best, dist);
            }
        }

        return best;
    }

    private static Vector2i GetTile(MapGridComponent grid, Vector2 localPos)
    {
        float size = grid.TileSize;
        return new Vector2i((int) MathF.Floor(localPos.X / size), (int) MathF.Floor(localPos.Y / size));
    }

    private bool HasTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        return _map.TryGetTileRef(gridUid, grid, tile, out var tileRef) && !tileRef.Tile.IsEmpty;
    }

    private bool IsOverTile(EntityUid gridUid, MapGridComponent grid, Vector2 localPos)
    {
        return HasTile(gridUid, grid, GetTile(grid, localPos));
    }

    /// <summary>
    /// Игрок числится ребёнком грида, но под ним нет тайла. Так бывает в зоне
    /// гистерезиса за краем грида: гравитации там быть не должно.
    /// </summary>
    private bool IsOverVoid(TransformComponent xform)
    {
        return _gridQuery.TryComp(xform.ParentUid, out var grid)
            && !IsOverTile(xform.ParentUid, grid, xform.LocalPosition);
    }

    private void OnIsWeightless(Entity<GravityAffectedComponent> ent, ref IsWeightlessEvent args)
    {
        var xform = Transform(ent);

        if (args.Handled || xform.MapUid is not { } map || !_gravQuery.HasComp(map) || !IsOverVoid(xform))
            return;

        args.Handled = true;
        args.IsWeightless = true;
    }

    /// <summary>
    /// Флаг невесомости кэшируется и сам не пересчитывается, когда игрок шагает
    /// с тайла на пустоту при том же родителе, поэтому пересчитываем его здесь.
    /// </summary>
    private void RefreshGravityState(EntityUid uid, TransformComponent xform)
    {
        if (!_affectedQuery.HasComp(uid) || !_gridQuery.HasComp(xform.ParentUid))
            return;

        var weightless = _gravity.IsWeightless(uid);
        var needRefresh = IsOverVoid(xform)
            ? !weightless
            : weightless && !_gravity.IsWeightlessStatusFromGrid(uid);

        if (needRefresh)
            _gravity.RefreshWeightless(uid);
    }
}
