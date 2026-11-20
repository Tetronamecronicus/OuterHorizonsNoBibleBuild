using System.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared._OuterHorizons.OrbitalGravity;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;


namespace Content.Client._OuterHorizons.ParticleSystem;

public class Particle
{
    public Vector2 position = Vector2.Zero;
    public TimeSpan spawnTime = TimeSpan.FromSeconds(0);
    public TimeSpan despawnTime = TimeSpan.FromSeconds(0);
    public float TotalRsiDuration;
    public string id = "Grassa1";
    public Robust.Client.Graphics.IRsiStateLike rsi;
    public SpriteSpecifier sprite;
    public int frameNum = 0;
    public bool animated = true;
    public EntityUid? parentGridUid = null;
    public TransformComponent? parentGrid = null;
    public Vector2 velocity = Vector2.Zero;
    public Vector2 zeroVelocity = Vector2.Zero;
    public float rotation = 0;
    public float angularVelocity = 0;
    public float damping = 0.1f;
    public float angularDamping = 0.1f;
    public bool rotateToVelocity = false;

    public Particle(TimeSpan spawnTime, float lifeTime, SpriteSpecifier sprite, SpriteSystem sprites)
    {
        this.spawnTime = spawnTime;
        despawnTime = spawnTime + TimeSpan.FromSeconds(lifeTime);
        this.sprite = sprite;

        rsi = sprites.RsiStateLike(sprite);
        float totalDelay = 0f;
        for (int i = 0; i < rsi.AnimationFrameCount; i++)
        {
            totalDelay += rsi.GetDelay(i);
        }
        TotalRsiDuration = totalDelay;
    }
}

