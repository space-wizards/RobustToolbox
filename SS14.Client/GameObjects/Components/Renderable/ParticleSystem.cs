/*
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    public class ParticleSystem
    {
        #region Classes
        private class Particle
        {
            /// <summary>
            /// Particle position relative to the emit position
            /// </summary>
            public Vector2 Position;

            /// <summary>
            /// Where the emitter was when the particle was first emitted
            /// </summary>
            public Vector2 EmitterPosition;

            /// <summary>
            /// Particle's x/y velocity
            /// </summary>
            public Vector2 Velocity;

            /// <summary>
            /// Particle's x/y acceleration
            /// </summary>
            public Vector2 Acceleration;

            /// <summary>
            /// Particle's radial velocity - relative to EmitterPosition
            /// </summary>
            public float RadialVelocity;

            /// <summary>
            /// Particle's radial acceleration
            /// </summary>
            public float RadialAcceleration;

            /// <summary>
            /// Particle's tangential velocity - relative to EmitterPosition
            /// </summary>
            public float TangentialVelocity;

            /// <summary>
            /// Particle's tangential acceleration
            /// </summary>
            public float TangentialAcceleration;

            /// <summary>
            /// Particle's age -- from 0f
            /// </summary>
            public float Age;

            /// <summary>
            /// Time after which the particle will "die"
            /// </summary>
            public float Lifetime;

            /// <summary>
            /// Particle's spin about its center in radians
            /// </summary>
            public float Spin;

            /// <summary>
            /// Rate of change of particle's spin
            /// </summary>
            public float SpinVelocity;

            /// <summary>
            /// Particle's current size
            /// </summary>
            public float Size;

            /// <summary>
            /// Rate of change of particle's size change
            /// </summary>
            public float SizeDelta;

            /// <summary>
            /// Particle's current color
            /// </summary>
            public Vector4 Color;

            /// <summary>
            /// Rate of change of particle's color
            /// </summary>
            public Vector4 ColorDelta;

            /// <summary>
            /// Whether or not the particle is currently being drawn
            /// </summary>
            public bool Alive;

            public Particle()
            {
                Position = ParticleDefaults.Position;
                EmitterPosition = ParticleDefaults.EmitterPosition;
                Velocity = ParticleDefaults.Velocity;
                Acceleration = ParticleDefaults.Acceleration;
                RadialVelocity = ParticleDefaults.RadialVelocity;
                RadialAcceleration = ParticleDefaults.RadialAcceleration;
                TangentialVelocity = ParticleDefaults.TangentialVelocity;
                TangentialAcceleration = ParticleDefaults.TangentialAcceleration;
                Age = ParticleDefaults.Age;
                Lifetime = ParticleDefaults.Lifetime;
                Spin = ParticleDefaults.Spin;
                SpinVelocity = ParticleDefaults.SpinVelocity;
                Size = ParticleDefaults.Size;
                SizeDelta = ParticleDefaults.SizeDelta;
                Color = ParticleDefaults.Color;
                ColorDelta = ParticleDefaults.ColorDelta;
                Alive = ParticleDefaults.Alive;
            }

            public void Update(float frameTime)
            {
                Age += frameTime;
                if (Age >= Lifetime)
                    Alive = false;
                Velocity += Acceleration*frameTime;
                RadialVelocity += RadialAcceleration*frameTime;
                TangentialVelocity += TangentialAcceleration*frameTime;

                //Calculate delta p due to radial velocity
                var positionRelativeToEmitter = Position - EmitterPosition;
                var deltaRadial = RadialVelocity * frameTime;
                var deltaPosition = positionRelativeToEmitter*(deltaRadial/positionRelativeToEmitter.Length);

                //Calculate delta p due to tangential velocity
                var radius = positionRelativeToEmitter.Length;
                if (radius > 0)
                {
                    var theta = (float)Math.Atan2(positionRelativeToEmitter.Y, positionRelativeToEmitter.X);
                    theta += TangentialVelocity*frameTime;
                    deltaPosition += new Vector2(radius * (float)Math.Cos(theta), radius*(float)Math.Sin(theta))
                                     - positionRelativeToEmitter;
                }
                //Calculate delta p due to Velocity
                deltaPosition += Velocity*frameTime;
                Position += deltaPosition;
                Spin += SpinVelocity*frameTime;
                Size += SizeDelta*frameTime;
                Color += ColorDelta*frameTime;
            }
        }

        private struct ParticleDefaults
        {
            /// <summary>
            /// Particle position relative to the emit position
            /// </summary>
            public static Vector2 Position = new Vector2(0,0);

            /// <summary>
            /// Where the emitter was when the particle was first emitted
            /// </summary>
            public static Vector2 EmitterPosition = new Vector2(0,0);

            /// <summary>
            /// Particle's x/y velocity
            /// </summary>
            public static Vector2 Velocity = new Vector2(0,0);

            /// <summary>
            /// Particle's x/y acceleration
            /// </summary>
            public static Vector2 Acceleration = new Vector2(0,0);

            /// <summary>
            /// Particle's radial velocity - relative to EmitterPosition
            /// </summary>
            public static float RadialVelocity = 1.0f;

            /// <summary>
            /// Particle's radial acceleration
            /// </summary>
            public static float RadialAcceleration = 0f;

            /// <summary>
            /// Particle's tangential velocity - relative to EmitterPosition
            /// </summary>
            public static float TangentialVelocity = 0f;

            /// <summary>
            /// Particle's tangential acceleration
            /// </summary>
            public static float TangentialAcceleration = 0f;

            /// <summary>
            /// Particle's age -- from 0f
            /// </summary>
            public static float Age = 0f;

            /// <summary>
            /// Time after which the particle will "die"
            /// </summary>
            public static float Lifetime = 1.0f;

            /// <summary>
            /// Particle's spin about its center in radians
            /// </summary>
            public static float Spin = 0f;

            /// <summary>
            /// Rate of change of particle's spin
            /// </summary>
            public static float SpinVelocity = 0f;

            /// <summary>
            /// Particle's current size
            /// </summary>
            public static float Size = 0f;

            /// <summary>
            /// Rate of change of particle's size change
            /// </summary>
            public static float SizeDelta = 0f;

            /// <summary>
            /// Particle's current color
            /// </summary>
            public static Vector4 Color = new Vector4(1,0,0,0);

            /// <summary>
            /// Rate of change of particle's color
            /// </summary>
            public static Vector4 ColorDelta = new Vector4(-1, 0, 0, 0);

            /// <summary>
            /// Whether or not the particle is currently being drawn
            /// </summary>
            public static bool Alive = false;
        }
        #endregion

        #region Variables

        private Particle[] _particles;
        private Random _rnd = new Random();
        private float _particlesToEmit;
        private List<Particle> _newParticles = new List<Particle>();

        #endregion

        #region Properties

        /// <summary>
        /// Emitter Position;
        /// This is the logical position of the emitter object
        /// </summary>
        public Vector2 EmitterPosition { get; set; }

        /// <summary>
        /// Emission Offset;
        /// This is where the particles should be emitted relative to the emitter position.
        /// </summary>
        public Vector2 EmissionOffset { get; set; }

        /// <summary>
        /// Emit Position
        /// This is where the particles will be emitted.
        /// </summary>
        public Vector2 EmitPosition { get { return EmitterPosition + EmissionOffset; } }

        /// <summary>
        /// Emit
        /// This controls whether particles are being emitted or not
        /// </summary>
        public bool Emit { get; set; }

        /// <summary>
        /// Emit Rate
        /// This controls the rate in particles per second at which particles are emitted.
        /// </summary>
        public int EmitRate { get; set; }

        /// <summary>
        /// Maximum Particles To Display
        /// This controls how many particles will be 'alive' at once. If the number of particles generated exceeds this,
        /// the oldest particles will be culled first.
        /// </summary>
        public int MaximumParticleCount
        {
            get { return _particles.Length; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("The number of particles cannot be less than 1.");

                _particles = new Particle[value];
                for (var i = 0; i < value; i++)
                {
                    _particles[i] = new Particle();
                }
            }
        }

        /// <summary>
        /// Particle Sprite
        /// This is the sprite that will be drawn as the "particle".
        /// </summary>
        public Sprite ParticleSprite { get; set; }

        /// <summary>
        /// Emission Radius
        /// This controls the range in radius from the emission position where the particles
        /// will start. If the radius is 0, they will all be emitted at the EmitPosition.
        /// </summary>
        public SS14.Shared.Utility.Range<float> EmissionRadiusRange { get; set; }

        /// <summary>
        /// Velocity Range
        /// This controls the particle's initial velocity
        /// </summary>
        public Vector2 Velocity { get; set; }

        /// <summary>
        /// Velocity Variance
        /// This controls the random variation of the particle's initial velocity
        /// </summary>
        public float VelocityVariance { get; set; }

        /// <summary>
        /// Acceleration Range
        /// This controls the particle's initial acceleration
        /// </summary>
        public Vector2 Acceleration { get; set; }

        /// <summary>
        /// Acceleration Variance
        /// This controls the random variation of particle's initial acceleration
        /// </summary>
        public float AccelerationVariance { get; set; }

        /// <summary>
        /// Radial Velocity Range
        /// This controls the particle's initial Radial velocity
        /// </summary>
        public float RadialVelocity { get; set; }

        /// <summary>
        /// Radial Velocity Variance
        /// Radial This controls the random variation of the particle's initial Radial velocity
        /// </summary>
        public float RadialVelocityVariance { get; set; }

        /// <summary>
        /// Radial Acceleration Range
        /// This controls the particle's initial Radial acceleration
        /// </summary>
        public float RadialAcceleration { get; set; }

        /// <summary>
        /// Radial Acceleration Variance
        /// This controls the random variation of particle's initial Radial acceleration
        /// </summary>
        public float RadialAccelerationVariance { get; set; }

        /// <summary>
        /// Tangential Velocity Range
        /// This controls the particle's initial tangential velocity
        /// </summary>
        public float TangentialVelocity { get; set; }

        /// <summary>
        /// Tangential Velocity Variance
        /// This controls the random variation of the particle's initial tangential velocity
        /// </summary>
        public float TangentialVelocityVariance { get; set; }

        /// <summary>
        /// Tangential Acceleration Range
        /// This controls the particle's initial tangential acceleration
        /// </summary>
        public float TangentialAcceleration { get; set; }

        /// <summary>
        /// Tangential Acceleration Variance
        /// This controls the random variation of particle's initial tangential acceleration
        /// </summary>
        public float TangentialAccelerationVariance { get; set; }

        /// <summary>
        /// Lifetime
        /// This controls the particle's lifetime
        /// </summary>
        public float Lifetime { get; set; }

        /// <summary>
        /// Lifetime Variance
        /// This controls the variation in the particle's lifetime
        /// </summary>
        public float LifetimeVariance { get; set; }

        /// <summary>
        /// Spin Velocity
        /// This controls the initial spin velocity over the life of the particle
        /// </summary>
        public SS14.Shared.Utility.Range<float> SpinVelocity { get; set; }

        /// <summary>
        /// Spin Velocity Variance
        /// This controls the random variation of the initial spin velocity of the particle
        /// </summary>
        public float SpinVelocityVariance { get; set; }

        /// <summary>
        /// Size Range
        /// This controls the range in size of the particle over the course of its lifetime
        /// </summary>
        public SS14.Shared.Utility.Range<float> SizeRange { get; set; }

        /// <summary>
        /// This controls how much particle size will vary between particles
        /// </summary>
        public float SizeVariance { get; set; }

        /// <summary>
        /// This controls the color range of the particle over the course of its lifetime
        /// </summary>
        public SS14.Shared.Utility.Range<Vector4> ColorRange { get; set; }

        /// <summary>
        /// This controls how much particle color will vary between particles
        /// </summary>
        public float ColorVariance { get; set; }

        /// <summary>
        /// Retrieves the set of live particles, ordered by oldest (nearest to death) first
        /// </summary>
        private IEnumerable<Particle> LiveParticles
        {
            get { return _particles.Where(p => p.Alive).OrderByDescending(p => 1-(p.Lifetime-p.Age)/p.Lifetime); }
        }

        /// <summary>
        /// Retrieves the set of dead particles
        /// </summary>
        private IEnumerable<Particle> DeadParticles
        {
            get { return _particles.Where(p => !p.Alive); }
        }

        private int DeadParticleCount
        {
            get { return DeadParticles.Count(); }
        }

        private int LiveParticleCount
        {
            get { return LiveParticles.Count(); }
        }
        #endregion

        #region Methods

        private float RandomFloat()
        {
            return (float) _rnd.NextDouble();
        }

        private float RandomSignedFloat()
        {
            return (float) (_rnd.NextDouble() - 0.5f)*2;
        }

        private float RandomRangeFloat(SS14.Shared.Utility.Range<float> randomRange)
        {
            return (RandomFloat() * (randomRange.End - randomRange.Start)) + randomRange.Start;
        }

        private float RandomRangeFloat(float start, float end)
        {
            return (RandomFloat()*(end - start)) + start;
        }

        private Vector2 RandomRangeVector2(SS14.Shared.Utility.Range<Vector2> randomRange)
        {
            return new Vector2(
                RandomRangeFloat(randomRange.Start.X, randomRange.End.X),
                RandomRangeFloat(randomRange.Start.Y, randomRange.End.Y)
                );
        }

        private Vector3 RandomRangeVector3(SS14.Shared.Utility.Range<Vector3> randomRange)
        {
            return new Vector3(
                RandomRangeFloat(randomRange.Start.X, randomRange.End.X),
                RandomRangeFloat(randomRange.Start.Y, randomRange.End.Y),
                RandomRangeFloat(randomRange.Start.Z, randomRange.End.Z)
                );
        }

        private Vector4 RandomRangeVector4(SS14.Shared.Utility.Range<Vector4> randomRange)
        {
            return new Vector4(
                RandomRangeFloat(randomRange.Start.X, randomRange.End.X),
                RandomRangeFloat(randomRange.Start.Y, randomRange.End.Y),
                RandomRangeFloat(randomRange.Start.Z, randomRange.End.Z),
                RandomRangeFloat(randomRange.Start.W, randomRange.End.W)
                );
        }

        private float VariedFloat(float value, float variance)
        {
            return value + RandomSignedFloat() * variance;
        }

        private float VariedPositiveFloat(float value, float variance)
        {
            var val = VariedFloat(value, variance);
            return Math.Max(val, 0);
        }

        private Vector2 VariedVector2(Vector2 value, float variance)
        {
            return new Vector2(
                VariedFloat(value.X, variance),
                VariedFloat(value.Y, variance)
                );
        }

        private Vector2 VariedPositiveVector2(Vector2 value, float variance)
        {
            return new Vector2(
                VariedPositiveFloat(value.X, variance),
                VariedPositiveFloat(value.Y, variance)
                );
        }

        private Vector3 VariedVector3(Vector3 value, float variance)
        {
            return new Vector3(
                VariedFloat(value.X, variance),
                VariedFloat(value.Y, variance),
                VariedFloat(value.Z, variance)
                );
        }

        private Vector3 VariedPositiveVector3(Vector3 value, float variance)
        {
            return new Vector3(
                VariedPositiveFloat(value.X, variance),
                VariedPositiveFloat(value.Y, variance),
                VariedPositiveFloat(value.Z, variance)
                );
        }

        private Vector4 VariedVector4(Vector4 value, float variance)
        {
            return new Vector4(
                VariedFloat(value.X, variance),
                VariedFloat(value.Y, variance),
                VariedFloat(value.Z, variance),
                VariedFloat(value.W, variance)
                );
        }

        private Vector4 VariedPositiveVector4(Vector4 value, float variance)
        {
            return new Vector4(
                VariedPositiveFloat(value.X, variance),
                VariedPositiveFloat(value.Y, variance),
                VariedPositiveFloat(value.Z, variance),
                VariedPositiveFloat(value.W, variance)
                );
        }

        private Vector4 Limit(Vector4 color)
        {
            if (Math.Max(color.X, Math.Max(color.Y, Math.Max(color.Z, color.W))) <= 255)
                return color;
            float x, y, z, w;
            x = color.X;
            y = color.Y;
            z = color.Z;
            w = color.W;
            //RGB Max
            var max = Math.Max(y, Math.Max(z, w));
            if(max > 255)
            {
                var f = 255/max;
                y = f*y;
                z = f*z;
                w = f*w;
            }
            if (x > 255)
                x = 255;
            return new Vector4(x, y, z, w);
        }

        private Color ToColor(Vector4 color)
        {
            color = Limit(color);

            return new Color((byte)color.W, (byte)color.X, (byte)color.Y, (byte)color.Z);
        }

        public void Start()
        {
            Emit = true;
        }

        private void EmitParticle(Particle p)
        {
            p.Acceleration = VariedVector2(Acceleration, AccelerationVariance);
            p.Lifetime = VariedPositiveFloat(Lifetime, LifetimeVariance);
            p.Age = 0;
            if (p.Lifetime == 0)
                return;
            p.Alive = true;
            p.Color = Limit(VariedPositiveVector4(ColorRange.Start, ColorVariance));
            var endColor = Limit(VariedPositiveVector4(ColorRange.End, ColorVariance));
            p.ColorDelta = (endColor - p.Color)/Lifetime;
            p.EmitterPosition = EmitPosition;
            var emitRadius = RandomRangeFloat(EmissionRadiusRange);
            emitRadius = emitRadius > 0.01f ? emitRadius : 0.1f;
            var emitAngle = RandomFloat()*2*Math.PI;
            p.Position = EmitPosition + new Vector2(
                                            emitRadius*(float)Math.Sin(emitAngle),
                                            emitRadius*(float)Math.Cos(emitAngle)
                                            );
            p.RadialAcceleration = VariedFloat(RadialAcceleration, RadialAccelerationVariance);
            p.RadialVelocity = VariedFloat(RadialVelocity, RadialVelocityVariance);
            p.Size = VariedPositiveFloat(SizeRange.Start, SizeVariance);
            var endSize = VariedPositiveFloat(SizeRange.End, SizeVariance);
            p.SizeDelta = (endSize - p.Size)/Lifetime;
            //TODO Add initial spin?
            p.SpinVelocity = VariedFloat(SpinVelocity.Start, SpinVelocityVariance);
            //TODO add spin velocity delta?
            p.TangentialAcceleration = VariedFloat(TangentialAcceleration, TangentialAccelerationVariance);
            p.TangentialVelocity = VariedFloat(TangentialVelocity, TangentialVelocityVariance);
            p.Velocity = VariedVector2(Velocity, VelocityVariance);
        }

        /// <summary>
        /// Copy given ParticleSettings into this emitter.
        /// </summary>
        /// <remarks>
        /// Applies the settings of the given ParticleSettings to this emitter.
        /// </remarks>
        /// <param name="toPosition"></param>
        public void LoadParticleSettings(ParticleSettings settings)
        {
            if (settings == null) return;

            this.Acceleration = settings.Acceleration;
            this.AccelerationVariance = settings.AccelerationVariance;

            //I hope this is correct.
            this.ColorRange = new SS14.Shared.Utility.Range<Vector4>(
                new Vector4(settings.ColorRange.Start.A, settings.ColorRange.Start.R, settings.ColorRange.Start.G, settings.ColorRange.Start.B),
                new Vector4(settings.ColorRange.End.A, settings.ColorRange.End.R, settings.ColorRange.End.G, settings.ColorRange.End.B));

            this.ColorVariance = settings.ColorVariance;
            this.EmissionOffset = settings.EmissionOffset;
            this.EmissionRadiusRange = new SS14.Shared.Utility.Range<float>(settings.EmissionRadiusRange.X, settings.EmissionRadiusRange.Y);
            this.EmitterPosition = settings.EmitterPosition;
            this.EmitRate = settings.EmitRate;
            this.Lifetime = settings.Lifetime;
            this.LifetimeVariance = settings.LifetimeVariance;
            this.MaximumParticleCount = settings.MaximumParticleCount;
            this.ParticleSprite = IoCManager.Resolve<IResourceCache>().GetSprite(settings.Sprite);
            this.RadialAcceleration = settings.RadialAcceleration;
            this.RadialAccelerationVariance = settings.RadialAccelerationVariance;
            this.RadialVelocity = settings.RadialVelocity;
            this.RadialVelocityVariance = settings.RadialVelocityVariance;
            this.SizeRange = new SS14.Shared.Utility.Range<float>(settings.SizeRange.X, settings.SizeRange.Y);
            this.SizeVariance = settings.SizeVariance;
            this.SpinVelocity = new SS14.Shared.Utility.Range<float>(settings.SpinVelocity.X, settings.SpinVelocity.Y);
            this.SpinVelocityVariance = settings.SpinVelocityVariance;
            this.TangentialAcceleration = settings.TangentialAcceleration;
            this.TangentialAccelerationVariance = settings.TangentialAccelerationVariance;
            this.TangentialVelocity = settings.TangentialVelocity;
            this.TangentialVelocityVariance = settings.TangentialVelocityVariance;
            this.Velocity = settings.Velocity;
            this.VelocityVariance = settings.VelocityVariance;
        }

        /// <summary>
        /// Move JUST the emitter
        /// </summary>
        /// <remarks>
        /// This moves the emitter's position
        /// </remarks>
        /// <param name="toPosition"></param>
        public void MoveEmitter(Vector2 toPosition)
        {
            EmitterPosition = toPosition;
        }

        /// <summary>
        /// Move JUST the particles, moving the emitter to offset
        /// </summary>
        /// <remarks>
        /// This moves the particles, but not the emitter. This changes the particles positions relative to the emitter.
        /// </remarks>
        /// <param name="toPosition"></param>
        public void MoveParticles(Vector2 toPosition)
        {
            var offset = toPosition - EmitterPosition;
            MoveParticlesOffset(offset);
        }

        /// <summary>
        /// Move JUST the particles, moving the emitter to offset
        /// </summary>
        /// <remarks>
        /// This moves the particles, but not the emitter. This changes the particles positions relative to the emitter.
        /// </remarks>
        /// <param name="offset"></param>
        public void MoveParticlesOffset(Vector2 offset)
        {
            Parallel.ForEach(LiveParticles, particle =>
            {
                particle.Position += offset;
                particle.EmitterPosition += offset;
            });
        }

        /// <summary>
        /// Move the whole system, both emitter and particles
        /// </summary>
        /// <remarks>
        /// Practically, this simply changes the emitter logical position. Since the particles are positioned relative to the
        /// emitter, they "move" as well.
        /// </remarks>
        /// <param name="toPosition"></param>
        public void Move(Vector2 toPosition)
        {
            MoveParticles(toPosition);
            EmitterPosition = toPosition;
        }

        public void Update(float frameTime)
        {
            foreach(var particle in LiveParticles)
            {
                particle.Update(frameTime);
            }

            if (!Emit)
                return;
            _particlesToEmit += frameTime*EmitRate;
            var newParticleCount = (int)Math.Floor(_particlesToEmit);
            //This should go down to zero.
            _particlesToEmit -= newParticleCount;

            //Clear out last update
            _newParticles.Clear();

            //Take some dead particles
            _newParticles.AddRange(DeadParticles.Take(newParticleCount));

            newParticleCount -= _newParticles.Count();

            //If there aren't enough dead ones, take the remainder from the live ones, oldest first.
            if(newParticleCount > 0)
                _newParticles.AddRange(LiveParticles.Take(newParticleCount));

            for (var i = 0; i < _newParticles.Count(); i++)
            {
                EmitParticle(_newParticles[i]);
            }
        }

        public void Render()
        {
            foreach (var particle in LiveParticles)
            {
                ParticleSprite.Color = ToColor(particle.Color);
                ParticleSprite.Position = particle.Position;
                ParticleSprite.Rotation = (float)(180f / Math.PI) * particle.Spin;
                ParticleSprite.Scale = new Vector2(particle.Size, particle.Size);
                ParticleSprite.Draw();
            }
        }
        #endregion

        #region Constructor/Destructors
        public ParticleSystem(Sprite particleSprite, Vector2 position)
        {
            MaximumParticleCount = 200;
            //TODO start with sane defaults
            Acceleration = new Vector2();
            AccelerationVariance = 0f;
            ColorRange = new SS14.Shared.Utility.Range<Vector4>(Vector4.UnitX * 255, Vector4.Zero);
            ColorVariance = 0f;
            EmissionOffset = new Vector2();
            EmissionRadiusRange = new SS14.Shared.Utility.Range<float>(0f, 0f);
            Emit = false;
            EmitRate = 1;
            EmitterPosition = position;
            Lifetime = 1.0f;
            LifetimeVariance = 0f;
            ParticleSprite = particleSprite;
            RadialAcceleration = 0f;
            RadialAccelerationVariance = 0f;
            RadialVelocity = 0f;
            RadialVelocityVariance = 0f;
            SizeRange = new SS14.Shared.Utility.Range<float>(1, 0);
            SizeVariance = 0.1f;
            SpinVelocity = new SS14.Shared.Utility.Range<float>(0f, 0f);
            SpinVelocityVariance = 0f;
            TangentialAcceleration = 0;
            TangentialAccelerationVariance = 0;
            TangentialVelocity = 0;
            TangentialVelocityVariance = 0;
            Velocity = new Vector2();
            VelocityVariance = 0;
        }
        public ParticleSystem(ParticleSettings settings, Vector2 position)
        {
            LoadParticleSettings(settings);
            EmitterPosition = position;
        }
        #endregion
    }
}
*/
