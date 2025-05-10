using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;


namespace Robust.Client.Graphics;

/// <summary>
///     System for creating and managing particle effects.
/// </summary>
[PublicAPI]
public sealed class ParticlesManager
{
    private Dictionary<EntityUid,ParticleSystem> _particleSystems = new();
    public void FrameUpdate(FrameEventArgs args)
    {
        // can't use parallel foreach here because IoC doesn't have context in parallel tasks
        foreach (var particleSys in _particleSystems.Values)
        {
            particleSys.FrameUpdate(args);
        }
    }

    public ParticleSystem CreateParticleSystem(EntityUid entity, ParticleSystemArgs args)
    {
        var newSystem = new ParticleSystem(args);
        _particleSystems.Add(entity, newSystem);
        return newSystem;
    }

    public void DestroyParticleSystem(EntityUid entity){
        _particleSystems.Remove(entity);
    }

    public bool TryGetParticleSystem(EntityUid entity, [NotNullWhen(true)] out ParticleSystem? system){
        return _particleSystems.TryGetValue(entity, out system);
    }
}

public sealed class ParticleSystem {

    //unchanging

    public Vector2i RenderSize { get => _particleSystemSize;}
    /// <summary>
    ///  Size of drawing surface
    /// </summary>
    private Vector2i _particleSystemSize;
    /// <summary>
    ///  Maximum number of particles in this system. New particles will not be created while at this maximum.
    /// </summary>
    private uint _particleCount;
    /// <summary>
    ///  The number of new particles to create each second. No new particles will be created if we are at the maximum already.
    /// </summary>
    private float _particlesPerSecond;
    /// <summary>
    ///  The lower left hand back corner of the cuboid outside of which particles will be deactivated
    /// </summary>
    private Vector3 _lowerBound;
    /// <summary>
    ///  The upper right hand front corner of the cuboid outside of which particles will be deactivated
    /// </summary>
    private Vector3 _upperBound;
    /// <summary>
    /// The base transform to apply to all particles in this system
    /// </summary>
    private Matrix3x2 _baseTransform;

    //queried on each particle spawn

    /// <summary>
    /// A function which returns a float which is this particles lifespan in seconds
    /// </summary>
    private Func<float> _lifespan;
    /// <summary>
    /// A function which returns a float which is this particles fade-out time in seconds
    /// </summary>
    private Func<float> _fadeout;
    /// <summary>
    /// A function which returns a float which is this particles fade-in time in seconds
    /// </summary>
    private Func<float> _fadein;
    /// <summary>
    /// A function which returns a Texture which is this particles texture at spawning. Null textures will be re-evaluated each frame until not null
    /// </summary>
    private Func<Texture?> _icon;
    /// <summary>
    /// A function which returns a Vector3 which is this particles position at spawning
    /// </summary>
    private Func<Vector3> _spawnPosition;
    /// <summary>
    /// A function which returns a Vector3 which is this particles velocity at spawning
    /// </summary>
    private Func<Vector3> _spawnVelocity;

    //queried every tick - arg is seconds particle has been alive. 0 for just spawned.

    /// <summary>
    /// A function which takes the life time of this particles and returns the Color of this particle
    /// </summary>
    private Func<float, Color> _color;
    /// <summary>
    /// A function which takes the life time of this particles and returns the transform of this particle. Note that this is multiplied with the base transform.
    /// </summary>
    private Func<float,Matrix3x2> _transform;
    /// <summary>
    /// A function which takes the life time of this particles and returns the an acceleration to apply to this particle
    /// </summary>
    private Func<float,Vector3,Vector3> _acceleration;

    /// <summary>
    /// Internal store for particles for this system
    /// </summary>
    private Particle[] _particles;


    public ParticleSystem(ParticleSystemArgs args)
    {
        _particleSystemSize = args.ParticleSystemSize;
        _particleCount = args.ParticleCount;
        _particlesPerSecond = args.ParticlesPerSecond;
        _lowerBound = args.LowerDrawBound is null ? new Vector3(-_particleSystemSize.X, -_particleSystemSize.Y, float.MinValue) : args.LowerDrawBound.Value;
        _upperBound = args.UpperDrawBound is null ? new Vector3(_particleSystemSize.X, _particleSystemSize.Y, float.MaxValue) : args.UpperDrawBound.Value;
        _icon = args.Icon;
        _baseTransform = args.BaseTransform is null ? Matrix3x2.Identity : args.BaseTransform.Value;
        _lifespan = args.Lifespan is null ? () => int.MaxValue : args.Lifespan;
        _fadeout = args.Fadeout is null ? () => 0 : args.Fadeout;
        _fadein = args.Fadein is null ? () => 0 : args.Fadein;
        _spawnPosition = args.SpawnPosition is null ? () => Vector3.Zero : args.SpawnPosition;
        _spawnVelocity = args.SpawnVelocity is null ? () => Vector3.Zero : args.SpawnVelocity;
        _color = args.Color is null ? (float lifetime) => Color.White : args.Color;
        _transform = args.Transform is null ? (float lifetime) => Matrix3x2.Identity : args.Transform;
        _acceleration = args.Acceleration is null ? (float lifetime, Vector3 velocity) => Vector3.Zero : args.Acceleration;

        _particles = new Particle[_particleCount];
        for(int i=0; i<_particleCount; i++)
            _particles[i] = new();
    }

    public void UpdateSystem(ParticleSystemArgs args){
        _particleSystemSize = args.ParticleSystemSize;
        if(_particleCount != args.ParticleCount){
            _particleCount = args.ParticleCount;
            Particle[] newParticles = new Particle[_particleCount];
            for(int i = 0; i <_particleCount; i++)
                if(i < _particles.Length)
                    newParticles[i] = _particles[i];
                else
                    newParticles[i] = new();
            _particles = newParticles;
        }
        _particlesPerSecond = args.ParticlesPerSecond;
        _lowerBound = args.LowerDrawBound is null ? new Vector3(-_particleSystemSize.X, -_particleSystemSize.Y, float.MinValue) : args.LowerDrawBound.Value;
        _upperBound = args.UpperDrawBound is null ? new Vector3(_particleSystemSize.X, _particleSystemSize.Y, float.MaxValue) : args.UpperDrawBound.Value;
        _icon = args.Icon;
        _baseTransform = args.BaseTransform is null ? Matrix3x2.Identity : args.BaseTransform.Value;
        _lifespan = args.Lifespan is null ? () => int.MaxValue : args.Lifespan;
        _fadeout = args.Fadeout is null ? () => 0 : args.Fadeout;
        _fadein = args.Fadein is null ? () => 0 : args.Fadein;
        _spawnPosition = args.SpawnPosition is null ? () => Vector3.Zero : args.SpawnPosition;
        _spawnVelocity = args.SpawnVelocity is null ? () => Vector3.Zero : args.SpawnVelocity;
        _color = args.Color is null ? (float lifetime) => System.Drawing.Color.White : args.Color;
        _transform = args.Transform is null ? (float lifetime) => Matrix3x2.Identity : args.Transform;
        _acceleration = args.Acceleration is null ? (float lifetime, Vector3 velocity) => Vector3.Zero : args.Acceleration;

    }

    public void FrameUpdate(FrameEventArgs args)
    {
        int particlesSpawned = 0;
        for(int i=0; i<_particleCount; i++){
            Particle p = _particles[i];
            if(p.active){
                p.lifetime += args.DeltaSeconds;
                p.transform = _baseTransform * _transform(p.lifetime);
                p.color = _color(p.lifetime);
                p.velocity += _acceleration(p.lifetime, p.velocity) * args.DeltaSeconds;
                p.position += p.velocity*args.DeltaSeconds;
                if(p.fadein > p.lifetime)
                    p.color.A = Math.Clamp(p.lifetime/p.fadein, 0, 1);
                if(p.fadeout > p.lifespan-p.lifetime)
                    p.color.A = Math.Clamp((p.lifespan-p.lifetime)/p.fadeout, 0, 1);

                if(p.lifetime > p.lifespan || p.position.X > _upperBound.X || p.position.Y > _upperBound.Y || p.position.Z > _upperBound.Z || p.position.X < _lowerBound.X || p.position.Y < _lowerBound.Y || p.position.Z < _lowerBound.Z)
                    p.active = false;

                if(p.texture is null)
                    p.texture = _icon();
            }
            if (!p.active && particlesSpawned < _particlesPerSecond*args.DeltaSeconds) {
                p.lifetime = 0;
                p.texture = _icon();
                p.position = _spawnPosition();
                p.velocity = _spawnVelocity();
                p.transform = _baseTransform * _transform(p.lifetime);
                p.color = _color(p.lifetime);
                p.lifespan = _lifespan();
                p.fadein = _fadein();
                p.fadeout = _fadeout();
                p.active = true;
                particlesSpawned++;
            }
        }
    }

    public void Draw(DrawingHandleWorld handle, Matrix3x2 transform){
        Array.Sort(_particles, (p1, p2) => p1.position.Z.CompareTo(p2.position.Z));
        foreach (var particle in _particles)
        {
            if(particle.active && particle.texture is not null){
                handle.SetTransform(particle.transform * transform);
                handle.DrawTextureRect(particle.texture!, new Box2(particle.position.Xy, (particle.position.Xy+particle.texture!.Size)), particle.color);
            }
        }
    }
}

internal sealed class Particle {
    public Texture? texture;
    public Vector3 position;
    public Vector3 velocity;
    public Matrix3x2 transform;
    public Color color;
    public float lifetime;
    public float lifespan;
    public float fadein;
    public float fadeout;
    public bool active;
}
