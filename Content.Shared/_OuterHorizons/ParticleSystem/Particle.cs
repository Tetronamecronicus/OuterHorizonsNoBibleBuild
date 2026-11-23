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
using Robust.Shared.Map;

namespace Content.Shared._OuterHorizons.ParticleSystem;



[Prototype("particle")]
public sealed partial class ParticlePrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables]
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [ViewVariables]
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ParticlePrototype>))]
    public string[]? Parents { get; private set; }


    [DataField("sprite"), AlwaysPushInheritance] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
    [DataField, AlwaysPushInheritance]
    public float damping = 0.1f;

    [DataField("zindex"), AlwaysPushInheritance]
    public int zindex = 0;
    [DataField, AlwaysPushInheritance]
    public float angulardamping = 0.1f;
    [DataField, AlwaysPushInheritance] public bool animated = true;
    [DataField, AlwaysPushInheritance] public bool below = false;
    [DataField, AlwaysPushInheritance] public bool rtv = false;
    [DataField, AlwaysPushInheritance] public float lifetime = 5;
    [DataField, AlwaysPushInheritance] public float rlifetime = 0;

    [DataField, AlwaysPushInheritance] public float scale = 1;
    [DataField, AlwaysPushInheritance] public float rscale = 0;
    [DataField("rbrightness"), AlwaysPushInheritance] public float rbrightness = 0;
    [DataField("nvelocity"), AlwaysPushInheritance] public float veloctiyNoice = 0;
    [DataField("navelocity"), AlwaysPushInheritance] public float angularVelocityNoice = 0;

    [DataField("noicespeed"), AlwaysPushInheritance] public float noicespeed = 0;

    [DataField("vispossnap"), AlwaysPushInheritance] public float visualPosSnap = 0;
    [DataField("visrotsnap"), AlwaysPushInheritance] public float visualRotSnap = 0;
    [DataField("visscalesnap"), AlwaysPushInheritance] public float visualScaleSnap = 0;
    [DataField("visfadesnap"), AlwaysPushInheritance] public float visualFadeSnap = 0;

    [DataField, AlwaysPushInheritance] public List<Vector2> fade = new List<Vector2>(){new Vector2(0,1)};
    [DataField, AlwaysPushInheritance] public List<Vector2> scalegrad = new List<Vector2>(){new Vector2(0,1)};
}



[Prototype("particlespawn")]
public sealed partial class ParticlesSpawnPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables]
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [ViewVariables]
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ParticlesSpawnPrototype>))]
    public string[]? Parents { get; private set; }

    [DataField, AlwaysPushInheritance] public int amount = 1;

    [DataField, AlwaysPushInheritance] public Vector2 position = Vector2.Zero;
    [DataField, AlwaysPushInheritance] public Vector2 velocity = Vector2.Zero;
    [DataField, AlwaysPushInheritance] public Vector2 force = Vector2.Zero;
    [DataField, AlwaysPushInheritance] public float rotation = 0;
    [DataField, AlwaysPushInheritance] public float avelocity = 0;

    [DataField, AlwaysPushInheritance] public float dirpos = 0;


    [DataField, AlwaysPushInheritance] public int stype = 0;  // 0 - linear    1 - sphere    2 - cone


    [DataField, AlwaysPushInheritance] public bool local = false;

    [DataField, AlwaysPushInheritance] public bool onlyspeed = false;
    [DataField, AlwaysPushInheritance] public float speed = 0;
    [DataField, AlwaysPushInheritance] public float dir = 0;
    [DataField, AlwaysPushInheritance] public float conespread = 45;



    [DataField, AlwaysPushInheritance] public int ramount = 0;
    [DataField, AlwaysPushInheritance] public Vector2 rvelocity = Vector2.Zero;
    [DataField, AlwaysPushInheritance] public Vector2 rposition = Vector2.Zero;
    [DataField, AlwaysPushInheritance] public float rrotation = 0;
    [DataField, AlwaysPushInheritance] public float ravelocity = 0;
    [DataField, AlwaysPushInheritance] public float rspeed = 0;
    [DataField, AlwaysPushInheritance] public float rdir = 0;
}




public class Gradient
{
    public List<Vector2> points = new List<Vector2>();

    public float Evaluate(float point)
    {
        if (points.Count == 0)
            return 0f;

        // 1. Обработка краев (если позиция вышла за пределы точек)
        if (points.Count == 1 || point <= points[0].X)
            return points[0].Y;

        if (point >= points[points.Count - 1].X)
            return points[points.Count - 1].Y;

        // 2. Поиск нужного отрезка [i, i + 1]
        // (Если точек много, обычный цикл можно заменить на бинарный поиск)
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[i + 1];

            if (point >= p0.X && point <= p1.X)
            {
                // Защита от деления на ноль, если две точки стоят на одной позиции X
                float length = p1.X - p0.X;
                if (length <= float.Epsilon)
                    return p0.Y;

                // Нормализуем позицию внутри отрезка в диапазон [0..1]
                float t = (point - p0.X) / length;

                // 3. Применяем плавную S-образную кривую (Smoothstep: 3t² - 2t³)
                // Производная на концах отрезка равна 0, что дает идеальную плавность стыков
                float smoothT = t * t * (3f - 2f * t);

                // Линейная интерполяция по сглаженному параметру
                return p0.Y + (p1.Y - p0.Y) * smoothT;
            }
        }

        return points[points.Count - 1].Y;
    }

    public Gradient(List<Vector2> points){
        this.points = points;
    }

    public static Gradient One()
    {
        return new Gradient(new List<Vector2>(){new Vector2(0,1)});
    }
    public static Gradient Zero()
    {
        return new Gradient(new List<Vector2>(){new Vector2(0,0)});
    }
    public static Gradient FadeIn()
    {
        return new Gradient(new List<Vector2>(){new Vector2(0,0),new Vector2(1,1)});
    }
    public static Gradient FadeOut()
    {
        return new Gradient(new List<Vector2>(){new Vector2(0,1),new Vector2(1,0)});
    }
}
public static class ShittyPerlinNoice
{
    public static float Pn(float x, float p, int o = 0)
    {
        float h = 0;
        if (o == 0)
        {
            h = MathF.Sin(x+p) + x;
        }
        else
        {
            h = o * MathF.Cos(x-2+p) + MathF.Sin(x+p) + x;
        }
        return MathF.Sin(h)*MathF.Cos(h/5f)*(MathF.Abs(MathF.Sin(h/25f))+0.5f);
    }
}

public class SpawnerData
{
    public SpawnerData(EntityUid uid)
    {
        this.uid = uid;
    }
    public EntityUid uid;
    
    public float? eyeAngle;
    public MapCoordinates? callerMapPos = null; 
    public MapCoordinates? perpaceMapPos = null; 
    public EntityUid? perpace = null;
    public Vector2? dirToPerpace = null;
    public float? floatdirToPerpace = null;
    public Vector2? viewDir = null;

    // сюда добавлять вообще всё что угодно, что необходимо для спавна
}

[ByRefEvent]
public readonly record struct SpawnParticlesEvent(EntityUid User, SpawnerData sd);
