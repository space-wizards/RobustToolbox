using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Robust.Client.Graphics;

[Prototype("particles")]
public sealed partial class ParticlesPrototype : IPrototype, ISerializationHooks
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("width", required: true)]
    public int Width { get; private set; } = default!;

    [DataField("height", required: true)]
    public int Height { get; private set; } = default!;

    [DataField("count", required: true)]
    public int Count { get; private set; } = default!;

    [DataField("spawning", required: true)]
    public int Spawning { get; private set; } = default!;

    [DataField("texture", required: false)]
    public List<ResPath> TextureList { get; private set; } = default!;

    [DataField("lifespan", required: false)]
    public GeneratorFloat Lifespan { get; private set; } = default!;

    [DataField("fadein", required: false)]
    public GeneratorFloat FadeIn { get; private set; } = default!;

    [DataField("fadeout", required: false)]
    public GeneratorFloat FadeOut { get; private set; } = default!;

    [DataField("color", required: false)]
    public List<string> ColorList { get; private set; } = default!;

    [DataField("spawn_position", required: false)]
    public GeneratorVector3 SpawnPosition { get; private set; } = default!;

    [DataField("spawn_velocity", required: false)]
    public GeneratorVector3 SpawnVelocity { get; private set; } = default!;

    [DataField("acceleration", required: false)]
    public GeneratorVector3 Acceleration { get; private set; } = default!;

    [DataField("scale", required: false)]
    public GeneratorVector2 Scale { get; private set; } = default!;

    [DataField("rotation", required: false)]
    public GeneratorFloat Rotation { get; private set; } = default!;

    [DataField("growth", required: false)]
    public GeneratorVector2 Growth { get; private set; } = default!;

    [DataField("spin", required: false)]
    public GeneratorFloat Spin { get; private set; } = default!;

    public ParticleSystemArgs GetParticleSystemArgs(IResourceCache resourceCache) {
        Func<Texture> textureFunc;
        if(TextureList is null || TextureList.Count == 0)
            textureFunc = () => Texture.White;
        else
            textureFunc = () => resourceCache.GetResource<TextureResource>(new Random().Pick(TextureList)); //TODO

        var result = new ParticleSystemArgs(textureFunc, new Vector2i(Width, Height), (uint)Count, Spawning);
        result.Lifespan = Lifespan.GetNext;
        result.Fadein = FadeIn.GetNext;
        result.Fadeout = FadeOut.GetNext;
        result.Color = (float lifetime) => {
            return (System.Drawing.Color)Color.FromHex(ColorList[0]); //TODO
        };
        result.Acceleration = (float lifetime) => Acceleration.GetNext();
        result.SpawnPosition = SpawnPosition.GetNext;
        result.SpawnVelocity = SpawnVelocity.GetNext;
        result.Transform = (float lifetime) => {
            var scale = Scale.GetNext();
            var rotation = Rotation.GetNext();
            var growth = Growth.GetNext();
            var spin = Spin.GetNext();
            return Matrix3x2.CreateScale(scale.X+growth.X, scale.Y+growth.Y) *
                    Matrix3x2.CreateRotation(rotation + spin);
        };

        return result;
    }
}

[DataDefinition]
public sealed partial class GeneratorFloat
{
    [DataField("type", required: true)]
    public string GeneratorType { get; private set; } = default!;

    [DataField("value", required: false)]
    public float Value { get; private set; } = default!;
    [DataField("low", required: false)]
    public float Low { get; private set; } = default!;
    [DataField("high", required: false)]
    public float High { get; private set; } = default!;

    private Random random = new();

    public float GetNext() {
        switch (GeneratorType) {
            case "constant":
                return Value;
            case "normal": //TODO
            case "uniform":
                return random.NextFloat(Low,High);
            default:
                throw new InvalidEnumArgumentException($"{GeneratorType} is not a valid generator type");
        }
    }
}

[DataDefinition]
public sealed partial class GeneratorVector2
{
    [DataField("type", required: true)]
    public string GeneratorType { get; private set; } = default!;

    [DataField("value", required: false)]
    public float[] Value { get; private set; } = default!;
    [DataField("low", required: false)]
    public float[] Low { get; private set; } = default!;
    [DataField("high", required: false)]
    public float[] High { get; private set; } = default!;
    private Random random = new();

    public Vector2 GetNext() {
        switch (GeneratorType) {
            case "constant":
                return new(Value[0], Value[1]);
            case "uniform":
                return new(random.NextFloat(Low[0],High[0]), random.NextFloat(Low[1],High[1]));
            default:
                throw new InvalidEnumArgumentException($"{GeneratorType} is not a valid generator type");
        }
    }
}

[DataDefinition]
public sealed partial class GeneratorVector3
{
    [DataField("type", required: true)]
    public string GeneratorType { get; private set; } = default!;

    [DataField("value", required: false)]
    public float[] Value { get; private set; } = default!;
    [DataField("low", required: false)]
    public float[] Low { get; private set; } = default!;
    [DataField("high", required: false)]
    public float[] High { get; private set; } = default!;
    private Random random = new();

    public Vector3 GetNext() {
        switch (GeneratorType) {
            case "constant":
                return new(Value[0],Value[1],Value[2]);
            case "uniform":
                return new(random.NextFloat(Low[0],High[0]), random.NextFloat(Low[1],High[1]), random.NextFloat(Low[2],High[2]));
            default:
                throw new InvalidEnumArgumentException($"{GeneratorType} is not a valid generator type");
        }
    }
}
