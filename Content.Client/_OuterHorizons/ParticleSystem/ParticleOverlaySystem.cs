using Robust.Client.Graphics;
using System.Numerics;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Client.Utility;
using Content.Shared._OuterHorizons.ParticleSystem;
using Content.Client.Light;
using Robust.Client.ResourceManagement;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._OuterHorizons.ParticleSystem;


public sealed partial class ParticleOverlaySystem : Overlay
{
    //[Dependency] private readonly IPrototypeManager _proto = default!;
    //[Dependency] private readonly IResourceCache _resCache = default!;
    private readonly SpriteSystem _sprites;
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototypeManager;

    private readonly ParticleSystem _ps;

    //public List<ParticlePrototype> cachedPrototypes = new List<ParticlePrototype>();
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    //public List<DecalPrototype> cachedDecalPrototypes = new List<DecalPrototype>();

    //public List<RSIResource> cachedRSIResources = new List<RSIResource>();





    public ParticleOverlaySystem(
        ParticleSystem ps,
        SpriteSystem sprites,
        IEntityManager entManager,
        IPrototypeManager prototypeManager)
    {
        _ps = ps;
        _sprites = sprites;
        _entManager = entManager;
        _prototypeManager = prototypeManager;
        //IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var handle = args.WorldHandle;
        var xformSystem = _entManager.System<TransformSystem>();
        var eyeAngle = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        foreach(Particle particle in _ps.particles)
        {
            //if (particle.decal.Sprite is not SpriteSpecifier.Rsi rsiSpecifier)
            //    continue;
            TransformComponent? xform = particle.parentGrid;
            Angle rot = Angle.Zero;

            if (xform != null)
            {
                if (xform.MapID != args.MapId) continue;
                //var gridAABB = xformSystem.GetInvWorldMatrix(xform).TransformBox(args.WorldBounds.Enlarged(1f));
                var (_, worldRot, worldMatrix) = xformSystem.GetWorldPositionRotationMatrix(xform);
                handle.SetTransform(worldMatrix);
                rot = worldRot;
            }
            else
            {
                handle.SetTransform(Matrix3x2.Identity);
            }

            //var rsi = _resCache.GetResource<RSIResource>("/Textures/"+rsiSpecifier.RsiPath).RSI;
            //if (!rsi.TryGetState(rsiSpecifier.RsiState, out var state))
            //    continue;

            //ParticlePrototype? dp = Contains(particle.id);
//
            //if (dp == null)
            //{
            //    // Nothing to cache someone messed up
            //    if (!_prototypeManager.TryIndex<ParticlePrototype>(particle.id, out var decalProto))
            //    {
            //        continue;
            //    }
            //    cachedPrototypes.Add(decalProto);
            //    dp = decalProto;
            //    //cache = (_sprites.Frame0(decalProto.Sprite), decalProto.SnapCardinals);
            //    //_cachedTextures[decal.Id] = cache;
            //}
            //DecalPrototype? dcp = ContainsDecal(dp.decal);
//
            //if (dcp == null)
            //{
            //    // Nothing to cache someone messed up
            //    if (!_prototypeManager.TryIndex<DecalPrototype>(dp.decal, out var decalProto))
            //    {
            //        continue;
            //    }
            //    cachedDecalPrototypes.Add(decalProto);
            //    dcp = decalProto;
            //    //cache = (_sprites.Frame0(decalProto.Sprite), decalProto.SnapCardinals);
            //    //_cachedTextures[decal.Id] = cache;
            //}
            var worldAngle = eyeAngle + rot;
            var cardinal = worldAngle.GetCardinalDir().ToAngle();

            var angle = Angle.FromDegrees(particle.rotation) - cardinal;
            var tex = particle.sprite.Frame0();//state.GetFrame(RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));//particle.decal.Sprite.Frame0();//particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.North,particle.frameNum);
            if (particle.rsi.AnimationFrameCount > 1)
            {
                tex = particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));
                //tex = particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));
            }
            //var tex = state.GetFrame(RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));//particle.decal.Sprite.Frame0();//particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.North,particle.frameNum);


            Vector2 pos = particle.position - ((Vector2)tex.Size/64f);
            //if (xform != null && particle.parentGridUid != null)
            //{
            //    EntityCoordinates coords = new EntityCoordinates(particle.parentGridUid.Value, pos);
            //    pos = coords.Position;
            //}
            //_sprites.RsiStateLike(particle.decal.Sprite);
            handle.DrawTexture(tex, pos, angle); // dp.Sprite.RsiStateLike().GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.North, particle.frameNum)
        }
    }

    /*
    public ParticlePrototype? Contains(string id)
    {
        foreach (ParticlePrototype a in cachedPrototypes)
        {
            if (a.ID == id) return a;
        }
        return null;
    }

    public RSIResource? ContainsRSI(string path)
    {
        foreach (RSIResource a in cachedRSIResources)
        {
            if (a.RSI.Path.Filename == path) return a;
        }
        return null;
    }


    public DecalPrototype? ContainsDecal(string id)
    {
        foreach (DecalPrototype a in cachedDecalPrototypes)
        {
            if (a.ID == id) return a;
        }
        return null;
    }*/
}
