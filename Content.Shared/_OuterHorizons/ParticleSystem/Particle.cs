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
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._OuterHorizons.ParticleSystem;



[Prototype("particle")]
public sealed partial class ParticlePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("sprite")] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
    [DataField]
    public float damping = 0.1f;
    [DataField]
    public float angulardamping = 0.1f;
    [DataField] public bool animated = true;
    [DataField] public bool rtv = false;
    [DataField] public float lifetime = 5;
    [DataField] public float rlifetime = 0;
}



[Prototype("particlespawn")]
public sealed partial class ParticlesSpawnPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;


    [DataField] public int amount = 1;

    [DataField] public Vector2 position = Vector2.Zero;
    [DataField] public Vector2 velocity = Vector2.Zero;
    [DataField] public float rotation = 0;
    [DataField] public float avelocity = 0;


    [DataField] public int stype = 0;  // 0 - linear    1 - sphere    2 - cone


    [DataField] public bool onlyspeed = false;
    [DataField] public float speed = 0;
    [DataField] public float dir = 0;
    [DataField] public float conespread = 45;



    [DataField] public int ramount = 0;
    [DataField] public Vector2 rvelocity = Vector2.Zero;
    [DataField] public Vector2 rposition = Vector2.Zero;
    [DataField] public float rrotation = 0;
    [DataField] public float ravelocity = 0;
    [DataField] public float rspeed = 0;
    [DataField] public float rdir = 0;
}
