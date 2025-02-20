using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Robust.Shared.GameObjects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DynamicParticlesComponent : Component {
	//The width and height of the drawing surface for the particles - this is the size of the texture that the particles are drawn on.
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int Width = 640;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int Height = 480;
	//Maximum number of particles in this system
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int Count = 1000;
	//Maximum number of particles spawned per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float Spawning = 100;
	//Texture that the particles have. This is a list of paths to textures, and the particle system will randomly choose one of them for each particle. If blank, the particle will be a 1px white dot.
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ResPath[] TextureList = [];
	//Maximum lifespan of the partcles in seconds
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float LifespanHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float LifespanLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType LifespanType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeInHigh = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeInLow = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType FadeInType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeOutHigh = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeOutLow = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType FadeOutType;
	//Color of the particles. This can either be a list of a colours, or a gradient that the particles will interpolate between over their lifespan.
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Color[] ColorList = [];
	//Starting position of the particles. X,Y,Z. The Z co-ordinate determines layering order.
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnPositionHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnPositionLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType SpawnPositionType;
	//Starting velocity of the particles
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnVelocityHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnVelocityLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType SpawnVelocityType;
	//Acceleration applied to the particles per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 AccelerationHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 AccelerationLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType AccelerationType;
	//Scaling applied to the particles in (x,y)
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 ScaleHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 ScaleLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType ScaleType;
	//Rotation applied to the particles in degrees
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float RotationHigh = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float RotationLow = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType RotationType;
	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 GrowthHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 GrowthLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType GrowthType;
	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float SpinHigh = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float SpinLow = 0;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType SpinType;
}

public enum ParticlePropertyType {
    HighValue,
    RandomUniform
}
