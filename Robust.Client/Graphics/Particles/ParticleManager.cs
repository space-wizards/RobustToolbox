using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Timing;


namespace Robust.Client.Graphics
{
    /// <summary>
    ///     System for creating and managing particle effects.
    /// </summary>
    [PublicAPI]
    public sealed class ParticleManager
    {
        private List<ParticleSystem> _particleSystems = new();
        public void FrameUpdate(FrameEventArgs args)
        {
            foreach (var particleSys in _particleSystems)
            {
                particleSys.FrameUpdate(args);
            }
        }

        public ParticleSystem CreateParticleSystem(ParticleSystemArgs args)
        {
            var newSystem = new ParticleSystem(args);
            _particleSystems.Add(newSystem);
            return newSystem;
        }

    }

    public sealed class ParticleSystem {

        //unchanging

        /// <summary>
        ///  Size of drawing surface
        /// </summary>
        private Vector2 _particleSystemSize;
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
        /// A function which returns a Texture which is this particles texture at spawning
        /// </summary>
        private Func<Texture> _icon;
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
        private Func<float,Color> _color;
        /// <summary>
        /// A function which takes the life time of this particles and returns the transform of this particle. Note that this is multiplied with the base transform.
        /// </summary>
        private Func<float,Matrix3x2> _transform;
        /// <summary>
        /// A function which takes the life time of this particles and returns the an acceleration to apply to this particle
        /// </summary>
        private Func<float,Vector3> _acceleration;

        /// <summary>
        /// Internal store for particles for this system
        /// </summary>
        private Particle[] _particles;


        public ParticleSystem(ParticleSystemArgs args)
        {
            _particleSystemSize = args.ParticleSystemSize;
            _particleCount = args.ParticleCount;
            _particlesPerSecond = args.ParticlesPerSecond;
            _lowerBound = args.LowerDrawBound is null ? new Vector3(_particleSystemSize, float.MinValue) : args.LowerDrawBound.Value;
            _upperBound = args.UpperDrawBound is null ? new Vector3(_particleSystemSize, float.MaxValue) : args.UpperDrawBound.Value;
            _icon = args.Icon;
            _baseTransform = args.BaseTransform is null ? Matrix3x2.Identity : args.BaseTransform.Value;
            _lifespan = args.Lifespan is null ? () => int.MaxValue : args.Lifespan;
            _fadeout = args.Fadeout is null ? () => 0 : args.Fadeout;
            _fadein = args.Fadein is null ? () => 0 : args.Fadein;
            _spawnPosition = args.SpawnPosition is null ? () => Vector3.Zero : args.SpawnPosition;
            _spawnVelocity = args.SpawnVelocity is null ? () => Vector3.Zero : args.SpawnVelocity;
            _color = args.Color is null ? (float lifetime) => Color.White : args.Color;
            _transform = args.Transform is null ? (float lifetime) => Matrix3x2.Identity : args.Transform;
            _acceleration = args.Acceleration is null ? (float lifetime) => Vector3.Zero : args.Acceleration;

            _particles = new Particle[_particleCount];
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
                    p.velocity += _acceleration(p.lifetime);
                    p.position += p.velocity*args.DeltaSeconds;
                    if(p.fadein > p.lifetime)
                        p.color = Color.FromArgb((int)Math.Clamp(p.lifetime/p.fadein * 255, 0, 255), p.color);
                    if(p.fadeout < p.lifespan-p.lifetime)
                        p.color = Color.FromArgb((int)Math.Clamp((p.lifespan-p.lifetime)/p.fadeout* 255, 0, 255), p.color);

                    if(p.lifetime > p.lifespan || p.position.X > _upperBound.X || p.position.Y > _upperBound.Y || p.position.Z > _upperBound.Z || p.position.X < _lowerBound.X || p.position.Y < _lowerBound.Y || p.position.Z < _lowerBound.Z)
                        p.active = false;
                } else if (particlesSpawned < _particlesPerSecond) {
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
                }
            }
        }

        public void Draw(in OverlayDrawArgs args){
            if (args.MapId == MapId.Nullspace)
                return;

            var handle = args.WorldHandle;
            foreach (var particle in _particles)
            {
                if(particle.active){
                    handle.SetTransform(particle.transform);
                    handle.DrawTexture(particle.texture, new Vector2(particle.position.X, particle.position.Y), particle.color);
                }
            }
        }
    }

    internal struct Particle {
        public Texture texture;
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
}
