using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;


namespace Robust.Client.Graphics;

/// <summary>
///     System for creating and managing particle effects.
/// </summary>
[PublicAPI]
public sealed class ParticlesManager
{
    private Dictionary<EntityUid,ParticleSystem> _particleSystems = new();
    public List<EntityUid> GetEntitiesWithParticles => _particleSystems.Keys.ToList();
    [Dependency] private IOverlayManager _overlayManager = default!;

    public void Initialize()
    {
        // enable when part 2 is merged
        // _overlayManager.AddOverlay(new ParticlesOverlay());
    }
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
            if(_particles[i].active){
                _particles[i].lifetime += args.DeltaSeconds;
                _particles[i].transform = _baseTransform * _transform(_particles[i].lifetime);
                _particles[i].color = _color(_particles[i].lifetime);
                _particles[i].velocity += _acceleration(_particles[i].lifetime, _particles[i].velocity) * args.DeltaSeconds;
                _particles[i].position += _particles[i].velocity*args.DeltaSeconds;
                if(_particles[i].fadein > _particles[i].lifetime)
                    _particles[i].color.A = Math.Clamp(_particles[i].lifetime/_particles[i].fadein, 0, 1);
                if(_particles[i].fadeout > _particles[i].lifespan-_particles[i].lifetime)
                    _particles[i].color.A = Math.Clamp((_particles[i].lifespan-_particles[i].lifetime)/_particles[i].fadeout, 0, 1);

                if(_particles[i].lifetime > _particles[i].lifespan || _particles[i].position.X > _upperBound.X || _particles[i].position.Y > _upperBound.Y || _particles[i].position.Z > _upperBound.Z || _particles[i].position.X < _lowerBound.X || _particles[i].position.Y < _lowerBound.Y || _particles[i].position.Z < _lowerBound.Z)
                    _particles[i].active = false;

                if(_particles[i].texture is null)
                    _particles[i].texture = _icon();
            }
            if (!_particles[i].active && particlesSpawned < _particlesPerSecond*args.DeltaSeconds) {
                _particles[i].lifetime = 0;
                _particles[i].texture = _icon();
                _particles[i].position = _spawnPosition();
                _particles[i].velocity = _spawnVelocity();
                _particles[i].transform = _baseTransform * _transform(_particles[i].lifetime);
                _particles[i].color = _color(_particles[i].lifetime);
                _particles[i].lifespan = _lifespan();
                _particles[i].fadein = _fadein();
                _particles[i].fadeout = _fadeout();
                _particles[i].active = true;
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
                handle.DrawTextureRect(particle.texture!, new Box2(new Vector2(particle.position.X, particle.position.Y), new Vector2(particle.position.X, particle.position.Y)+particle.texture!.Size), particle.color);
            }
        }
    }
}

internal struct Particle {
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
