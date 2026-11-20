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

namespace Content.Client._OuterHorizons.ParticleSystem;

public sealed partial class ParticleSpawnerSystem : EntitySystem
{

    [Dependency] private ParticleSystem _particleSystem = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private int frame;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleSpawnerComponent, ComponentInit>(OnSpawnerAdded);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var spawnersQuery = EntityQueryEnumerator<ParticleSpawnerComponent>();
        frame += 1;
        if (frame > 180)
        {
            frame = 0;
        }
        else
        {
            return;
        }
        while (spawnersQuery.MoveNext(out var uid, out var comp))
        {
            /*
            public Vector2 position = Vector2.Zero;
            public string id = "Grassa1";
            public int frameNum = 0;
            public EntityUid? parentGridUid = null;
            public TransformComponent? parentGrid = null;
            public Vector2 velocity = Vector2.Zero;
            public Vector2 zeroVelocity = Vector2.Zero;
            public float rotation = 0;
            public float angularVelocity = 0;
            public float damping = 0.1f;
            public float angularDamping = 0.1f;
            */
            /*
            Particle particle = new Particle(_timing.CurTime, 5);
            TransformComponent? xform = _entityManager.GetComponent<TransformComponent>(uid);
            EntityUid? grid = xform.GridUid;
            TransformComponent? tform = null;

            particle.velocity = new Vector2(5,0);
            particle.rotation = 0;
            if (grid != null)
            {
                tform = _entityManager.GetComponent<TransformComponent>(grid.Value);
                //PhysicsComponent? phys = _entityManager.GetComponent<PhysicsComponent>(grid.Value);
                //particle.zeroVelocity = phys.LinearVelocity;
            }
            particle.position = xform.LocalPosition - new Vector2(0.5f,0.5f);
            particle.parentGrid = tform;
            particle.parentGridUid = grid;
            particle.id = comp.id;
            particle.frameNum = 0;
            particle.angularVelocity = 0;

            AddParticle(uid, particle);
            */


            AddParticles(uid,CreateParticles(uid,comp));
        }
    }


    private void OnSpawnerAdded(EntityUid uid, ParticleSpawnerComponent comp, ref ComponentInit args)
    {
        AddParticles(uid,CreateParticles(uid,comp));
    }


    public void AddParticles(EntityUid uid, List<Particle> addParticles)
    {
        _particleSystem.AddParticles(uid, addParticles);
    }

    /*public Particle CreateParticle(EntityUid uid, TransformComponent xform, Vector2 velocity, ParticleSpawnerComponent? comp)
    {
        Particle particle = new Particle();
        EntityUid? grid = xform.GridUid;
        TransformComponent? tform = null;

        particle.velocity = velocity;
        particle.rotation = 0;
        if (grid != null)
        {
            tform = _entityManager.GetComponent<TransformComponent>(grid.Value);
            PhysicsComponent? phys = _entityManager.GetComponent<PhysicsComponent>(grid.Value);
            particle.zeroVelocity = phys.LinearVelocity;
        }
        particle.position = xform.LocalPosition;
        particle.parentGrid = tform;
        particle.parentGridUid = grid;
        if (comp != null) particle.id = comp.id;
        particle.frameNum = 0;
        particle.angularVelocity = 0;

        return particle;
    }*/

    public List<Particle> CreateParticles(EntityUid uid, ParticleSpawnerComponent comp)
    {
        if(!_prototypeManager.TryIndex<ParticlePrototype>(comp.paritcle, out ParticlePrototype? proto)) return new List<Particle>();
        if(!_prototypeManager.TryIndex<ParticlesSpawnPrototype>(comp.spawnType, out ParticlesSpawnPrototype? spawnproto)) return new List<Particle>();
        return CreateParticles(uid, proto, spawnproto);
    }

    public List<Particle> CreateParticles(EntityUid uid, ParticlePrototype proto, ParticlesSpawnPrototype spawnproto)
    {
        List<Particle> particles = new List<Particle>();



        TransformComponent? xform =_entityManager.GetComponent<TransformComponent>(uid);
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

        for (int i = 0; i < amount; i++)
        {
            Particle particle = new Particle(_timing.CurTime, proto.lifetime + (_random.NextFloat(proto.rlifetime)-proto.rlifetime/2f), proto.Sprite, _sprites);

            particle.velocity = new Vector2(_random.NextFloat(spawnproto.rvelocity.X)-spawnproto.rvelocity.X/2f, _random.NextFloat(spawnproto.rvelocity.Y)-spawnproto.rvelocity.Y/2f);//new Vector2(MathF.Cos((float)i/15f * MathF.PI), MathF.Sin((float)i/15f * MathF.PI));
            if (spawnproto.onlyspeed == true) particle.velocity += spawnproto.velocity;
            float dir = 0;
            Vector2 dirV = Vector2.One;
            if (spawnproto.stype == 0)
            {
                dir = spawnproto.dir + (_random.NextFloat(spawnproto.rdir)-spawnproto.rdir/2f);
            }
            else if (spawnproto.stype == 1)
            {
                dir = (float)i/(float)amount*360f;
            }
            else if (spawnproto.stype == 2)
            {
                dir = spawnproto.dir + (_random.NextFloat(spawnproto.rdir)-spawnproto.rdir/2f) + ((float)i/(float)amount-0.5f)*(spawnproto.conespread/2f);
            }
            dirV = new Vector2(MathF.Cos(dir/180f*MathF.PI), MathF.Sin(dir/180f*MathF.PI));
            particle.velocity += dirV * (spawnproto.speed + (_random.NextFloat(spawnproto.rspeed)-spawnproto.rspeed/2f));
            if (grid == null && phys != null)
            {
                particle.zeroVelocity = phys.LinearVelocity;
                //particle.velocity += phys.LinearVelocity;
            }
            particle.rotation = spawnproto.rotation + _random.NextFloat(spawnproto.rrotation)-spawnproto.rrotation/2f;
            particle.position = spawnproto.position + xform.LocalPosition     +  new Vector2(_random.NextFloat(spawnproto.rposition.X)-spawnproto.rposition.X/2f, _random.NextFloat(spawnproto.rposition.Y)-spawnproto.rposition.Y/2f)       ;
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






            particles.Add(particle);
        }





        return particles;
    }


    public void AddParticle(EntityUid uid, Particle particle)
    {
        List<Particle> l = new List<Particle>();
        l.Add(particle);
        AddParticles(uid,l);
    }
}
