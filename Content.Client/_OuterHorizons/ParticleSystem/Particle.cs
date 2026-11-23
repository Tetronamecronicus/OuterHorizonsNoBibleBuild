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
using Content.Shared._OuterHorizons.ParticleSystem;

namespace Content.Client._OuterHorizons.ParticleSystem;

public class Particle
{
    public string id = "Grassa1";

    // sprite
    public SpriteSpecifier sprite;
    public Robust.Client.Graphics.IRsiStateLike rsi;
    public float TotalRsiDuration;
    public int frameNum = 0;
    public int zindex = 0;
    public int index = 0;
    public bool animated = true;
    public bool rotateToVelocity = false;

    public float visualPosSnap = 0;
    public float visualRotSnap = 0;
    public float visualScaleSnap = 0;
    public float visualFadeSnap = 0;
    public bool belowEntitys = false;
    public float brightness = 1;

    // main params
    public TimeSpan spawnTime = TimeSpan.FromSeconds(0);
    public TimeSpan despawnTime = TimeSpan.FromSeconds(0);
    public EntityUid? parentGridUid = null;
    public TransformComponent? parentGrid = null;


    // particle params
    public Vector2 position = Vector2.Zero;
    public float rotation = 0;
    public float scale = 1;


    // visuals
    public Gradient fade = Gradient.One();
    public Gradient scaleMult = Gradient.One();

    // movement
    public Vector2 velocity = Vector2.Zero;
    public float angularVelocity = 0;


    public Vector2 force = Vector2.Zero;


    public float damping = 0.1f;
    public Vector2 zeroVelocity = Vector2.Zero;
    public float angularDamping = 0.1f;


    // noice

    public float noiceSpeed = 0;

    public float veloctiyNoice = 0;
    public float angularVelocityNoice = 0;




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

