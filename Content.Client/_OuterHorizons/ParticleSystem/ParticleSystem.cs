using System.Numerics;
using Content.Shared._OuterHorizons.ParticleSystem;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client._OuterHorizons.ParticleSystem;


public sealed partial class ParticleSystem : EntitySystem
{
    private ISawmill _sawmill = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private IGameTiming _timing = default!;

    public List<Particle> particles = new List<Particle>();
    public int particlesSpawn = 0;



    // Максимальное количество партиклов
    public int maxParticles = 1000;

    private ParticleOverlaySystem? _overlay;
    private ParticleBelowOverlaySystem? _overlayBelow;

    private int frame;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new ParticleOverlaySystem(this, _sprites, EntityManager, ProtoMan, _timing);
        _overlayManager.AddOverlay(_overlay);
        _overlayBelow = new ParticleBelowOverlaySystem(this, _sprites, EntityManager, ProtoMan, _timing);
        _overlayManager.AddOverlay(_overlayBelow);

        _sawmill = _logManager.GetSawmill("oh.particle_system");
        _sawmill.Info("Работает");
    }
    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlayManager.RemoveOverlay(_overlay);
        if (_overlayBelow != null)
            _overlayManager.RemoveOverlay(_overlayBelow);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        frame += 1;
        if (frame > 1000)
        {
            frame = 0;
            _sawmill.Info("Количество партиклов: "+particles.Count);
        }

        List<Particle> newParticles = new List<Particle>();

        // обновление каждого партикла

        foreach (Particle p in particles)
        {
            if (_timing.CurTime > p.despawnTime) continue;
            if(p.parentGrid!=null){
                p.velocity = p.velocity + (p.zeroVelocity - p.velocity) * p.damping * frameTime;//(p.zeroVelocity - p.velocity) * p.damping;
            }
            p.velocity += p.force * frameTime;
            if (p.veloctiyNoice != 0)
            {
                p.velocity += new Vector2(ShittyPerlinNoice.Pn(p.position.X+(float)_timing.CurTime.TotalSeconds*25 * p.noiceSpeed, (float)_timing.CurTime.TotalSeconds * p.noiceSpeed),ShittyPerlinNoice.Pn(p.position.Y+(float)_timing.CurTime.TotalSeconds*25 * p.noiceSpeed+10, (float)_timing.CurTime.TotalSeconds * p.noiceSpeed+10)) * p.veloctiyNoice * frameTime;
            }

            p.position += p.velocity * frameTime;

            if (p.rotateToVelocity)
            {
                p.angularVelocity = 0;
                Vector2 vel = p.velocity.Normalized();
                p.rotation = MathF.Atan2(vel.Y, vel.X) * (180f / MathF.PI);
            }
            else
            {  
                if(p.parentGrid!=null){
                    p.angularVelocity = p.angularVelocity - p.angularVelocity * p.angularDamping * frameTime;
                }
                if (p.angularVelocityNoice != 0)
                {
                    p.angularVelocity += ShittyPerlinNoice.Pn(p.rotation+(float)_timing.CurTime.TotalSeconds*25 * p.noiceSpeed+25, (float)_timing.CurTime.TotalSeconds * p.noiceSpeed+25)*frameTime*p.angularVelocityNoice;
                }
                p.rotation += p.angularVelocity * frameTime;
            }
            if (p.rotation >= 360)
            {
                p.rotation = p.rotation - 360;
            }
            if (p.rotation < 0)
            {
                p.rotation = p.rotation + 360;
            }



            // анимация. не смотри сюда
            if (p.animated){
                p.frameNum = 0;
                var state = p.rsi;
                if (state.AnimationFrameCount <= 1 || p.TotalRsiDuration <= 0f)
                {
                    p.frameNum = 0;
                }
                else
                {
                    var totalLife = (p.despawnTime - p.spawnTime).TotalSeconds;
                    if (totalLife <= 0f)
                    {
                        p.frameNum = 0;
                    }
                    else
                    {
                        var elapsed = (_timing.CurTime - p.spawnTime).TotalSeconds;
                        var progress = Math.Clamp(elapsed / totalLife, 0.0, 0.9999);

                        var targetRsiTime = (float)(progress * p.TotalRsiDuration);

                        float accumulated = 0f;
                        for (int i = 0; i < state.AnimationFrameCount; i++)
                        {
                            accumulated += state.GetDelay(i);
                            if (targetRsiTime < accumulated)
                            {
                                p.frameNum = i;
                                break;
                            }
                        }

                    }
                }
            }

            newParticles.Add(p);
        }
        particles = newParticles;
    }


    public void AddParticles(EntityUid uid, List<Particle> addParticles)
    {
        foreach (Particle p in addParticles)
        {
            if (particles.Count >= maxParticles - 1)
            {
                particles.RemoveAt(0);
            }
            p.index = particlesSpawn;
            particles.Add(p);
            particlesSpawn += 1;
            //_sawmill.Error("Добавлен партикл: "+p.id+" "+p.frameNum+" "+p.position+" "+p.rotation+" "+p.velocity+" "+p.parentGridUid);
            //_sawmill.Error("Количество партиклов: "+particles.Count);
        }
        particles.Sort((x, y) =>
        {
            var zComp = x.zindex.CompareTo(y.zindex);

            if (zComp != 0)
                return zComp;

            return x.index.CompareTo(y.index);
        });
    }
}
