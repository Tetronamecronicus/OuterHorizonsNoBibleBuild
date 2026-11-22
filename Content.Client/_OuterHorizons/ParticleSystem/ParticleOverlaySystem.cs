using Robust.Client.Graphics;
using System.Numerics;
using Content.Shared.Decals;
using Content.Shared.DrawDepth;
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
using Robust.Shared.Timing;

namespace Content.Client._OuterHorizons.ParticleSystem;


public sealed partial class ParticleOverlaySystem : Overlay
{
    private readonly SpriteSystem _sprites;
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly IGameTiming _timing;

    private readonly ParticleSystem _ps;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public ParticleOverlaySystem(
        ParticleSystem ps,
        SpriteSystem sprites,
        IEntityManager entManager,
        IPrototypeManager prototypeManager,
        IGameTiming timing)
    {
        _ps = ps;
        _sprites = sprites;
        _entManager = entManager;
        _prototypeManager = prototypeManager;
        _timing = timing;
        ZIndex = (int)Content.Shared.DrawDepth.DrawDepth.Overdoors;
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
            if(particle.belowEntitys) continue;
            TransformComponent? xform = particle.parentGrid;
            Angle rot = Angle.Zero;

            if (xform != null)
            {
                if (xform.MapID != args.MapId) continue;
                var (_, worldRot, worldMatrix) = xformSystem.GetWorldPositionRotationMatrix(xform);
                handle.SetTransform(worldMatrix);
                rot = worldRot;
            }
            else
            {
                handle.SetTransform(Matrix3x2.Identity);
            }

            var worldAngle =  rot;
            Vector2 particlePos = particle.position;
            float particlerot = particle.rotation;
            if (particle.visualRotSnap != 0)
            {
                particlerot = MathF.Round(particlerot * (1/particle.visualRotSnap)) / (1/particle.visualRotSnap);
            }
            if (particle.visualPosSnap != 0&& particle.parentGrid != null)
            {
                particlePos = new Vector2(MathF.Round(particlePos.X * (1/particle.visualPosSnap)) / (1/particle.visualPosSnap),MathF.Round(particlePos.Y * (1/particle.visualPosSnap)) / (1/particle.visualPosSnap));
            }
            var angle = Angle.FromDegrees(particlerot);
            if (!particle.rotateToVelocity)
            {
                angle -= worldAngle;
            }
            var tex = particle.sprite.Frame0();
            if (particle.rsi.AnimationFrameCount > 1)
            {
                tex = particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));
                //tex = particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));
            }
            float plife = (float)((_timing.CurTime.TotalSeconds - particle.spawnTime.TotalSeconds)/(particle.despawnTime.TotalSeconds - particle.spawnTime.TotalSeconds));
            float alpha = particle.fade.Evaluate(plife);
            if (particle.visualFadeSnap != 0)
            {
                alpha = MathF.Round(alpha * (1/particle.visualFadeSnap)) / (1/particle.visualFadeSnap);
            }
            
            Color color = new Color(particle.brightness,particle.brightness,particle.brightness, alpha);
            //var tex = state.GetFrame(RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));//particle.decal.Sprite.Frame0();//particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.North,particle.frameNum);
            Vector2 scale = (Vector2)tex.Size / 32f;
            if (particle.visualScaleSnap != 0)
            {
                scale *= MathF.Round(particle.scale * particle.scaleMult.Evaluate(plife) * (1/particle.visualScaleSnap)) / (1/particle.visualScaleSnap);
                scale = new Vector2(MathF.Abs(scale.X),MathF.Abs(scale.Y));
            }
            Box2Rotated pos = new Box2Rotated(new Box2(particlePos - scale/2f, particlePos + scale/2f), angle, particlePos);

            handle.DrawTextureRect(tex, pos, color);
        }
    }

}

public sealed partial class ParticleBelowOverlaySystem : Overlay
{
    private readonly SpriteSystem _sprites;
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly IGameTiming _timing;

    private readonly ParticleSystem _ps;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public ParticleBelowOverlaySystem(
        ParticleSystem ps,
        SpriteSystem sprites,
        IEntityManager entManager,
        IPrototypeManager prototypeManager,
        IGameTiming timing)
    {
        _ps = ps;
        _sprites = sprites;
        _entManager = entManager;
        _prototypeManager = prototypeManager;
        _timing = timing;
        ZIndex = (int)Content.Shared.DrawDepth.DrawDepth.BelowMobs;
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
            if(!particle.belowEntitys) continue;
            TransformComponent? xform = particle.parentGrid;
            Angle rot = Angle.Zero;

            if (xform != null)
            {
                if (xform.MapID != args.MapId) continue;
                var (_, worldRot, worldMatrix) = xformSystem.GetWorldPositionRotationMatrix(xform);
                handle.SetTransform(worldMatrix);
                rot = worldRot;
            }
            else
            {
                handle.SetTransform(Matrix3x2.Identity);
            }

            var worldAngle =  rot;
            Vector2 particlePos = particle.position;
            float particlerot = particle.rotation;
            if (particle.visualRotSnap != 0)
            {
                particlerot = MathF.Round(particlerot * (1/particle.visualRotSnap)) / (1/particle.visualRotSnap);
            }
            if (particle.visualPosSnap != 0&& particle.parentGrid != null)
            {
                particlePos = new Vector2(MathF.Round(particlePos.X * (1/particle.visualPosSnap)) / (1/particle.visualPosSnap),MathF.Round(particlePos.Y * (1/particle.visualPosSnap)) / (1/particle.visualPosSnap));
            }
            var angle = Angle.FromDegrees(particlerot);
            if (!particle.rotateToVelocity)
            {
                angle -= worldAngle;
            }
            var tex = particle.sprite.Frame0();
            if (particle.rsi.AnimationFrameCount > 1)
            {
                tex = particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));
                //tex = particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));
            }
            float plife = (float)((_timing.CurTime.TotalSeconds - particle.spawnTime.TotalSeconds)/(particle.despawnTime.TotalSeconds - particle.spawnTime.TotalSeconds));
            float alpha = particle.fade.Evaluate(plife);
            if (particle.visualFadeSnap != 0)
            {
                alpha = MathF.Round(alpha * (1/particle.visualFadeSnap)) / (1/particle.visualFadeSnap);
            }
            
            Color color = new Color(particle.brightness,particle.brightness,particle.brightness, alpha);
            //var tex = state.GetFrame(RsiDirection.South,(int)MathF.Max(0,(int)MathF.Min(particle.rsi.AnimationFrameCount-1,particle.frameNum)));//particle.decal.Sprite.Frame0();//particle.rsi.GetFrame(Robust.Shared.Graphics.RSI.RsiDirection.North,particle.frameNum);
            Vector2 scale = (Vector2)tex.Size / 32f;
            if (particle.visualScaleSnap != 0)
            {
                scale *= MathF.Round(particle.scale * particle.scaleMult.Evaluate(plife) * (1/particle.visualScaleSnap)) / (1/particle.visualScaleSnap);
                scale = new Vector2(MathF.Abs(scale.X),MathF.Abs(scale.Y));
            }
            Box2Rotated pos = new Box2Rotated(new Box2(particlePos - scale/2f, particlePos + scale/2f), angle, particlePos);

            handle.DrawTextureRect(tex, pos, color);
        }
    }

}
