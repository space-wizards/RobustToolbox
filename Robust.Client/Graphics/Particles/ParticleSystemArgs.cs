using System;
using System.Drawing;
using System.Numerics;
using Robust.Shared.Maths;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.Graphics;

/// <summary>
///     Arguments for creating a particle system
/// </summary>
public sealed class ParticleSystemArgs {
    /// <summary>
    ///  Size of drawing surface
    /// </summary>
    public Vector2i ParticleSystemSize;
    /// <summary>
    ///  Maximum number of particles in this system. New particles will not be created while at this maximum.
    /// </summary>
    public uint ParticleCount;
    /// <summary>
    ///  The number of new particles to create each second. No new particles will be created if we are at the maximum already.
    /// </summary>
    public float ParticlesPerSecond;
    /// <summary>
    ///  The lower left hand back corner of the cuboid outside of which particles will be deactivated
    /// </summary>
    public Vector3?  LowerDrawBound;
    /// <summary>
    ///  The upper right hand front corner of the cuboid outside of which particles will be deactivated
    /// </summary>
    public Vector3? UpperDrawBound;
    /// <summary>
    /// The base transform to apply to all particles in this system
    /// </summary>
    public Matrix3x2? BaseTransform;

    //queried on each particle spawn

    /// <summary>
    /// A function which returns a float which is this particles lifespan in seconds. Defaults to infinity.
    /// </summary>
    public Func<float>? Lifespan;
    /// <summary>
    /// A function which returns a float which is this particles fade-out time in seconds
    /// </summary>
    public Func<float>? Fadeout;
    /// <summary>
    /// A function which returns a float which is this particles fade-in time in seconds
    /// </summary>
    public Func<float>? Fadein;
    /// <summary>
    /// A function which returns a Texture which is this particles texture at spawning
    /// </summary>
    public Func<Texture?> Icon;
    /// <summary>
    /// A function which returns a Vector3 which is this particles position at spawning
    /// </summary>
    public Func<Vector3>? SpawnPosition;
    /// <summary>
    /// A function which returns a Vector3 which is this particles velocity at spawning
    /// </summary>
    public Func<Vector3>? SpawnVelocity;

    //queried every tick - arg is seconds particle has been alive. 0 for just spawned.

    /// <summary>
    /// A function which takes the life time of this particles and returns the Color of this particle
    /// </summary>
    public Func<float,Color>? Color;
    /// <summary>
    /// A function which takes the life time of this particles and returns the transform of this particle. Note that this is multiplied with the base transform.
    /// </summary>
    public Func<float,Matrix3x2>? Transform;
    /// <summary>
    /// A function which takes the life time of this particles and returns the an acceleration to apply to this particle
    /// </summary>
    public Func<float,Vector3,Vector3>? Acceleration;

    public ParticleSystemArgs(Func<Texture?> icon, Vector2i particleSystemSize, uint particleCount, float particlesPerSecond){
        Icon = icon;
        ParticleSystemSize = particleSystemSize;
        ParticleCount = particleCount;
        ParticlesPerSecond = particlesPerSecond;
    }

}
