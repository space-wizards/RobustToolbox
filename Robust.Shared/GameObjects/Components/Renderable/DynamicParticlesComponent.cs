using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Robust.Shared.GameObjects;

[NetworkedComponent]
public abstract partial class SharedDynamicParticlesComponent : Component {
	//The width and height of the drawing surface for the particles - this is the size of the texture that the particles are drawn on.
    [ViewVariables(VVAccess.ReadWrite)] public int Width = 640;
    [ViewVariables(VVAccess.ReadWrite)] public int Height = 480;
	//Maximum number of particles in this system
    [ViewVariables(VVAccess.ReadWrite)] public int Count = 1000;
	//Maximum number of particles spawned per second
    [ViewVariables(VVAccess.ReadWrite)] public float Spawning = 100;
	//Texture that the particles have. This is a list of paths to textures, and the particle system will randomly choose one of them for each particle. If blank, the particle will be a 1px white dot.
    [ViewVariables(VVAccess.ReadWrite)] public ResPath[] TextureList = [];
	//Maximum lifespan of the partcles in seconds
    [ViewVariables(VVAccess.ReadWrite)] public float LifespanHigh;
    [ViewVariables(VVAccess.ReadWrite)] public float LifespanLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType LifespanType;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeInHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeInLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType FadeInType;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeOutHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeOutLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType FadeOutType;
	//Color of the particles. This can either be a list of a colours, or a gradient that the particles will interpolate between over their lifespan.
    [ViewVariables(VVAccess.ReadWrite)] public Color[] ColorList = [];
	//Starting position of the particles. X,Y,Z. The Z co-ordinate determines layering order.
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnPositionHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnPositionLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpawnPositionType;
	//Starting velocity of the particles
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnVelocityHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnVelocityLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpawnVelocityType;
	//Acceleration applied to the particles per second
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 AccelerationHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 AccelerationLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType AccelerationType;
	//Scaling applied to the particles in (x,y)
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 ScaleHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 ScaleLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType ScaleType;
	//Rotation applied to the particles in degrees
    [ViewVariables(VVAccess.ReadWrite)] public float RotationHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public float RotationLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType RotationType;
	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 GrowthHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 GrowthLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType GrowthType;
	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite)] public float SpinHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public float SpinLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpinType;
}

public enum ParticlePropertyType {
    HighValue,
    RandomUniform
}
