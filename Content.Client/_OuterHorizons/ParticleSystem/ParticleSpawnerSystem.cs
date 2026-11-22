using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Content.Shared._OuterHorizons.ParticleSystem;
using Robust.Shared.Prototypes;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Client.Utility;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;


namespace Content.Client._OuterHorizons.ParticleSystem;



public sealed partial class ParticleSpawnerSystem : EntitySystem
{

    private ISawmill _sawmill = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private ParticleSystem _particleSystem = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleOnSpawnSpawnerComponent, ComponentInit>(OnSpawnerAdded);
        SubscribeLocalEvent<ParticleOnHitSpawnerComponent, DamageDealtEvent>(OnSpawnerDamaged);
        SubscribeLocalEvent<ParticleOnDestroySpawnerComponent, DestructionAttemptEvent>(OnSpawnerDestroyed);
        SubscribeLocalEvent<ParticleOnCommandSpawnerComponent, SpawnParticlesEvent>(OnSpawnerCommanded);

        _sawmill = _logManager.GetSawmill("oh.particle_system.spawner");
        _sawmill.Info("Работает");
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var spawnersQuery = EntityQueryEnumerator<ParticlePeriodicSpawnerComponent>();

        while (spawnersQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.nextSpawn < _timing.CurTime)
            {
                comp.nextSpawn = _timing.CurTime + TimeSpan.FromSeconds(comp.every);
                SpawnerData sd = new SpawnerData(uid);
                sd.eyeAngle = eyeRot(uid);
                AddParticles(uid,CreateParticles(uid,comp, sd));
            }

            /*


            Это вот использовать для спавна партиклов.   вставляешь это в код какой-нибудь системы где хочешь
            и тогда, объект, который вызвал этот ивент, то если у него есть компонент ParticleOnCommandSpawnerComponent, то он заспавнит партиклы


            SpawnerData sd2 = new SpawnerData(uid);
            SpawnParticlesEvent spawnParticles = new SpawnParticlesEvent(uid, sd2);
            RaiseLocalEvent(uid, ref spawnParticles);
            
            
            */
        }
    }


    private void OnSpawnerAdded(EntityUid uid, ParticleOnSpawnSpawnerComponent comp, ref ComponentInit args)
    {
        SpawnerData sd = new SpawnerData(uid);
        sd.eyeAngle = eyeRot(uid);
        AddParticles(uid,CreateParticles(uid,comp, sd));
    }
    private void OnSpawnerDamaged(EntityUid uid, ParticleOnHitSpawnerComponent comp, ref DamageDealtEvent args)
    {
        SpawnerData sd = new SpawnerData(uid);
        sd.eyeAngle = eyeRot(uid);
        if (args.Origin != null){
            Vector2 uidCoords = _transform.GetMapCoordinates(uid).Position;
            Vector2 callerCoords = _transform.GetMapCoordinates(args.Origin.Value).Position;
            Vector2 dir = (uidCoords-callerCoords).Normalized();
            sd.perpace = args.Origin.Value;
            sd.callerMapPos = _transform.GetMapCoordinates(uid);
            sd.perpaceMapPos = _transform.GetMapCoordinates(args.Origin.Value);
            sd.dirToPerpace = dir;
            sd.floatdirToPerpace = MathF.Atan2(dir.Y, dir.X) * (180f / MathF.PI);
        }
        AddParticles(uid,CreateParticles(uid,comp, sd));
    }
    private void OnSpawnerDestroyed(EntityUid uid, ParticleOnDestroySpawnerComponent comp, ref DestructionAttemptEvent args)
    {
        SpawnerData sd = new SpawnerData(uid);
        sd.eyeAngle = eyeRot(uid);
        AddParticles(uid,CreateParticles(uid,comp, sd));
    }
    private void OnSpawnerCommanded(EntityUid uid, ParticleOnCommandSpawnerComponent comp, ref SpawnParticlesEvent args)
    {
        SpawnerData sd = args.sd;
        sd.eyeAngle = eyeRot(uid);
        AddParticles(uid,CreateParticles(uid,comp, sd));
    }

    private float eyeRot(EntityUid uid)
    {
        var exform = _entityManager.GetComponent<TransformComponent>(uid);
        if(exform.GridUid!=null){
            var xform = _entityManager.GetComponent<TransformComponent>(exform.GridUid.Value);//Transform(uid);
            var gridWorldRot = xform.LocalRotation;
            return (float)(-_eyeManager.CurrentEye.Rotation - gridWorldRot).Reduced().Degrees;
        }
        //_sawmill.Error("Почему-то не было грида?");
        return -(float)_eyeManager.CurrentEye.Rotation.Degrees;
    }


    public List<Particle> CreateParticles(EntityUid uid, ParticleOnHitSpawnerComponent comp, SpawnerData sd)
    {
        return CreateParticles(uid, comp.toSpawn, sd);
    }
    public List<Particle> CreateParticles(EntityUid uid, ParticleOnCommandSpawnerComponent comp, SpawnerData sd)
    {
        return CreateParticles(uid, comp.toSpawn, sd);
    }
    public List<Particle> CreateParticles(EntityUid uid, ParticleOnDestroySpawnerComponent comp, SpawnerData sd)
    {
        return CreateParticles(uid, comp.toSpawn, sd);
    }
    public List<Particle> CreateParticles(EntityUid uid, ParticlePeriodicSpawnerComponent comp, SpawnerData sd)
    {
        return CreateParticles(uid, comp.toSpawn, sd);
    }
    public List<Particle> CreateParticles(EntityUid uid, ParticleOnSpawnSpawnerComponent comp, SpawnerData sd)
    {
        return CreateParticles(uid, comp.toSpawn, sd);
    }
    public List<Particle> CreateParticles(EntityUid uid, string toS, SpawnerData sd)
    {
        string[] toSpawn = toS.Replace(" ","").Split(";");
        List<Particle> particles = new List<Particle>();
        foreach (string particleInfo in toSpawn)
        {
            string[] info = particleInfo.Split(',');
            if(!_prototypeManager.TryIndex<ParticlePrototype>(info[0], out ParticlePrototype? proto)) return new List<Particle>();
            if(!_prototypeManager.TryIndex<ParticlesSpawnPrototype>(info[1], out ParticlesSpawnPrototype? spawnproto)) return new List<Particle>();
            particles.AddRange(CreateParticles(uid, proto, spawnproto, sd));
        }
        return particles;
    }
    public List<Particle> CreateParticles(EntityUid uid, ParticlePrototype proto, ParticlesSpawnPrototype spawnproto, SpawnerData sd)
    {
        List<Particle> particles = new List<Particle>();



        TransformComponent? xform =_entityManager.GetComponent<TransformComponent>(uid);

        Vector2 spawnerRelativePos = xform.MapPosition.Position - _eyeManager.CurrentEye.Position.Position;
        if (MathF.Abs(spawnerRelativePos.X)>_eyeManager.CurrentEye.Scale.X || MathF.Abs(spawnerRelativePos.Y)>_eyeManager.CurrentEye.Scale.Y)
            return particles;

        EntityUid? grid = xform.GridUid;
        TransformComponent? tform = null;
        PhysicsComponent? phys = null;
        if (grid != null)
            tform = _entityManager.GetComponent<TransformComponent>(grid.Value);
        if (grid == null)
        {
            phys = _entityManager.GetComponent<PhysicsComponent>(uid);
        }

        int amount = spawnproto.amount + (_random.Next(spawnproto.ramount+1)-(int)MathF.Round((float)spawnproto.ramount/2f));
        //_sawmill.Info("Spawn Particles with eyerot: "+sd.eyeAngle+"    "+_eyeManager.CurrentEye.Rotation+" "+(tform!=null?tform.LocalRotation:null));
        for (int i = 0; i < amount; i++)
        {
            Particle particle = new Particle(_timing.CurTime, proto.lifetime + (_random.NextFloat(proto.rlifetime)-proto.rlifetime/2f), proto.Sprite, _sprites);

            particle.velocity = new Vector2(_random.NextFloat(spawnproto.rvelocity.X)-spawnproto.rvelocity.X/2f, _random.NextFloat(spawnproto.rvelocity.Y)-spawnproto.rvelocity.Y/2f);//new Vector2(MathF.Cos((float)i/15f * MathF.PI), MathF.Sin((float)i/15f * MathF.PI));
            if (spawnproto.onlyspeed == true) particle.velocity += spawnproto.velocity;
            float dir = spawnproto.dir;
            if (sd.floatdirToPerpace != null)
            {
                dir = sd.floatdirToPerpace.Value;
            }
            dir += (_random.NextFloat(spawnproto.rdir)-spawnproto.rdir/2f);
            if (spawnproto.local && sd.eyeAngle!=null)
            {
                dir += (float)sd.eyeAngle.Value;
            }
            Vector2 dirV = Vector2.One;
            if (spawnproto.stype == 0)
            {
                //
            }
            else if (spawnproto.stype == 1)
            {
                dir = (float)i/(float)amount*360f;
            }
            else if (spawnproto.stype == 2)
            {
                dir += ((float)i/(float)amount-0.5f)*(spawnproto.conespread/2f);
            }
            dirV = new Vector2(MathF.Cos(dir/180f*MathF.PI), MathF.Sin(dir/180f*MathF.PI));
            particle.velocity += dirV * (spawnproto.speed + (_random.NextFloat(spawnproto.rspeed)-spawnproto.rspeed/2f));
            if (grid == null && phys != null)
            {
                particle.zeroVelocity = phys.LinearVelocity* ((float)_timing.FrameTime.TotalSeconds*2);
                particle.velocity += phys.LinearVelocity* ((float)_timing.FrameTime.TotalSeconds*2);
            }
            float rotation = spawnproto.rotation + _random.NextFloat(spawnproto.rrotation)-spawnproto.rrotation/2f;
            Vector2 position = spawnproto.position + new Vector2(_random.NextFloat(spawnproto.rposition.X)-spawnproto.rposition.X/2f, _random.NextFloat(spawnproto.rposition.Y)-spawnproto.rposition.Y/2f);
            particle.force = spawnproto.force;
            if (spawnproto.local && sd.eyeAngle!=null)
            {
                position = RotateVector(position, +sd.eyeAngle.Value);
                //particle.velocity = RotateVector(particle.velocity, -sd.eyeAngle.Value);
                rotation += (float)sd.eyeAngle.Value;
                particle.force = RotateVector(spawnproto.force, +sd.eyeAngle.Value);
            }

            particle.rotation = rotation;
            particle.position = xform.LocalPosition + dirV * spawnproto.dirpos + position;
            particle.parentGrid = tform;
            particle.parentGridUid = grid;
            particle.id = proto.ID;
            particle.frameNum = 0;
            particle.angularVelocity = spawnproto.avelocity + _random.NextFloat(spawnproto.ravelocity)-spawnproto.ravelocity/2f;
            particle.damping = proto.damping;
            particle.angularDamping = proto.angulardamping;
            particle.rotateToVelocity = proto.rtv;
            particle.animated = proto.animated;
            if (proto.animated == false)
            {
                particle.frameNum = _random.Next(particle.rsi.AnimationFrameCount);
            }
            particle.scale = proto.scale + (_random.NextFloat(proto.rscale)-proto.rscale/2f);
            particle.noiceSpeed = proto.noicespeed;
            particle.angularVelocityNoice = proto.angularVelocityNoice;
            particle.veloctiyNoice = proto.veloctiyNoice;
            particle.fade = new Gradient(proto.fade);
            particle.scaleMult = new Gradient(proto.scalegrad);
            particle.visualPosSnap = proto.visualPosSnap;
            particle.visualRotSnap = proto.visualRotSnap;
            particle.visualScaleSnap = proto.visualScaleSnap;
            particle.visualFadeSnap = proto.visualFadeSnap;
            particle.belowEntitys = proto.below;
            particle.brightness = MathF.Max(0,1-(_random.NextFloat(proto.rbrightness)-proto.rbrightness/2f));
            particle.zindex = proto.zindex;

            particles.Add(particle);
        }
        return particles;
    }
    private Vector2 RotateVector(Vector2 v, float angle)
    {
        float radians = angle/180f*MathF.PI;
        float sin = MathF.Sin(radians);
        float cos = MathF.Cos(radians);
        
        return new Vector2(
            v.X * cos - v.Y * sin,
            v.X * sin + v.Y * cos
        );
    }
    public void AddParticles(EntityUid uid, List<Particle> addParticles)
    {
        _particleSystem.AddParticles(uid, addParticles);
    }
}

