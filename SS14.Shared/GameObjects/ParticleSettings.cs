using SS14.Shared.Utility;
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class ParticleSettings : INotifyPropertyChanged
    {
        private Vector2 _emitterPosition = new Vector2(0, 0);
        private Vector2 _emissionOffset = new Vector2(0, 0);
        private int _emitRate = 1;
        private int _maximumParticleCount = 200;
        private Vector2 _emissionRadiusRange = new Vector2(0, 0);
        private Vector2 _velocity = new Vector2(0, 0);
        private float _velocityVariance = 0;
        private Vector2 _acceleration = new Vector2(0, 0);
        private float _accelerationVariance = 0;
        private float _radialVelocity = 0;
        private float _radialVelocityVariance = 0;
        private float _radialAcceleration = 0;
        private float _colorVariance = 0;
        private float _sizeVariance = 0;
        private Vector2 _sizeRange = new Vector2(1, 1);
        private float _spinVelocityVariance = 0;
        private Vector2 _spinVelocity = new Vector2(0, 0);
        private float _lifetimeVariance = 0;
        private float _lifetime = 1;
        private float _tangentialAccelerationVariance = 0;
        private float _tangentialAcceleration = 0;
        private float _tangentialVelocityVariance = 0;
        private float _tangentialVelocity = 0;
        private float _radialAccelerationVariance = 0;
        private Range<Color> _colorRange = new Range<Color>(Color.Black, new Color(0, 0, 0, 0));
        private string _sprite = "star1";

        public ParticleSettings()
        {
        }

        public void Load(ParticleSettings loadFrom)
        {
            Acceleration = loadFrom.Acceleration;
            AccelerationVariance = loadFrom.AccelerationVariance;
            ColorRange = loadFrom.ColorRange;
            ColorVariance = loadFrom.ColorVariance;
            EmissionOffset = loadFrom.EmissionOffset;
            EmissionRadiusRange = loadFrom.EmissionRadiusRange;
            EmitRate = loadFrom.EmitRate;
            EmitterPosition = loadFrom.EmitterPosition;
            Lifetime = loadFrom.Lifetime;
            LifetimeVariance = loadFrom.LifetimeVariance;
            MaximumParticleCount = loadFrom.MaximumParticleCount;
            RadialAcceleration = loadFrom.RadialAcceleration;
            RadialAccelerationVariance = loadFrom.RadialAccelerationVariance;
            RadialVelocity = loadFrom.RadialVelocity;
            RadialVelocityVariance = loadFrom.RadialVelocityVariance;
            SizeRange = loadFrom.SizeRange;
            SizeVariance = loadFrom.SizeVariance;
            SpinVelocity = loadFrom.SpinVelocity;
            SpinVelocityVariance = loadFrom.SpinVelocityVariance;
            Sprite = loadFrom.Sprite;
            TangentialAcceleration = loadFrom.TangentialAcceleration;
            TangentialAccelerationVariance = loadFrom.TangentialAccelerationVariance;
            TangentialVelocity = loadFrom.TangentialVelocity;
            TangentialVelocityVariance = loadFrom.TangentialVelocityVariance;
            Velocity = loadFrom.Velocity;
            VelocityVariance = loadFrom.VelocityVariance;
        }

        /// <summary>
        /// Sprite
        /// This is the selected sprite to display.
        /// </summary>
        public string Sprite
        {
            get { return _sprite; }
            set
            {
                _sprite = value;
                OnPropertyChanged("Sprite");
            }
        }

        /// <summary>
        /// Emitter Position;
        /// This is the logical position of the emitter object
        /// </summary>
        public Vector2 EmitterPosition
        {
            get { return _emitterPosition; }
            set
            {
                _emitterPosition = value;
                OnPropertyChanged("EmitterPosition");
            }
        }

        /// <summary>
        /// Emission Offset;
        /// This is where the particles should be emitted relative to the emitter position.
        /// </summary>
        public Vector2 EmissionOffset
        {
            get { return _emissionOffset; }
            set
            {
                _emissionOffset = value;
                OnPropertyChanged("EmissionOffset");
            }
        }

        /// <summary>
        /// Emit Rate
        /// This controls the rate in particles per second at which particles are emitted.
        /// </summary>
        public int EmitRate
        {
            get { return _emitRate; }
            set
            {
                _emitRate = value;
                OnPropertyChanged("EmitRate");
            }
        }

        /// <summary>
        /// Maximum Particles To Display
        /// This controls how many particles will be 'alive' at once. If the number of particles generated exceeds this,
        /// the oldest particles will be culled first.
        /// </summary>
        public int MaximumParticleCount
        {
            get { return _maximumParticleCount; }
            set
            {
                _maximumParticleCount = value;
                OnPropertyChanged("MaximumParticleCount");
            }
        }

        /// <summary>
        /// Emission Radius
        /// This controls the range in radius from the emission position where the particles
        /// will start. If the radius is 0, they will all be emitted at the EmitPosition.
        /// </summary>
        public Vector2 EmissionRadiusRange
        {
            get { return _emissionRadiusRange; }
            set
            {
                _emissionRadiusRange = value;
                OnPropertyChanged("EmissionRadiusRange");
            }
        }

        /// <summary>
        /// Velocity Range
        /// This controls the particle's initial velocity
        /// </summary>
        public Vector2 Velocity
        {
            get { return _velocity; }
            set
            {
                _velocity = value;
                OnPropertyChanged("Velocity");
            }
        }

        /// <summary>
        /// Velocity Variance
        /// This controls the random variation of the particle's initial velocity
        /// </summary>
        public float VelocityVariance
        {
            get { return _velocityVariance; }
            set
            {
                _velocityVariance = value;
                OnPropertyChanged("VelocityVariance");
            }
        }

        /// <summary>
        /// Acceleration Range
        /// This controls the particle's initial acceleration
        /// </summary>
        public Vector2 Acceleration
        {
            get { return _acceleration; }
            set
            {
                _acceleration = value;
                OnPropertyChanged("Acceleration");
            }
        }

        /// <summary>
        /// Acceleration Variance
        /// This controls the random variation of particle's initial acceleration
        /// </summary>
        public float AccelerationVariance
        {
            get { return _accelerationVariance; }
            set
            {
                _accelerationVariance = value;
                OnPropertyChanged("AccelerationVariance");
            }
        }

        /// <summary>
        /// Radial Velocity Range
        /// This controls the particle's initial Radial velocity
        /// </summary>
        public float RadialVelocity
        {
            get { return _radialVelocity; }
            set
            {
                _radialVelocity = value;
                OnPropertyChanged("RadialVelocity");
            }
        }

        /// <summary>
        /// Radial Velocity Variance
        /// Radial This controls the random variation of the particle's initial Radial velocity
        /// </summary>
        public float RadialVelocityVariance
        {
            get { return _radialVelocityVariance; }
            set
            {
                _radialVelocityVariance = value;
                OnPropertyChanged("RadialVelocityVariance");
            }
        }

        /// <summary>
        /// Radial Acceleration Range
        /// This controls the particle's initial Radial acceleration
        /// </summary>
        public float RadialAcceleration
        {
            get { return _radialAcceleration; }
            set
            {
                _radialAcceleration = value;
                OnPropertyChanged("RadialAcceleration");
            }
        }

        /// <summary>
        /// Radial Acceleration Variance
        /// This controls the random variation of particle's initial Radial acceleration
        /// </summary>
        public float RadialAccelerationVariance
        {
            get { return _radialAccelerationVariance; }
            set
            {
                _radialAccelerationVariance = value;
                OnPropertyChanged("RadialAccelerationVariance");
            }
        }

        /// <summary>
        /// Tangential Velocity Range
        /// This controls the particle's initial tangential velocity
        /// </summary>
        public float TangentialVelocity
        {
            get { return _tangentialVelocity; }
            set
            {
                _tangentialVelocity = value;
                OnPropertyChanged("TangentialVelocity");
            }
        }

        /// <summary>
        /// Tangential Velocity Variance
        /// This controls the random variation of the particle's initial tangential velocity
        /// </summary>
        public float TangentialVelocityVariance
        {
            get { return _tangentialVelocityVariance; }
            set
            {
                _tangentialVelocityVariance = value;
                OnPropertyChanged("TangentialVelocityVariance");
            }
        }

        /// <summary>
        /// Tangential Acceleration Range
        /// This controls the particle's initial tangential acceleration
        /// </summary>
        public float TangentialAcceleration
        {
            get { return _tangentialAcceleration; }
            set
            {
                _tangentialAcceleration = value;
                OnPropertyChanged("TangentialAcceleration");
            }
        }

        /// <summary>
        /// Tangential Acceleration Variance
        /// This controls the random variation of particle's initial tangential acceleration
        /// </summary>
        public float TangentialAccelerationVariance
        {
            get { return _tangentialAccelerationVariance; }
            set
            {
                _tangentialAccelerationVariance = value;
                OnPropertyChanged("TangentialAccelerationVariance");
            }
        }

        /// <summary>
        /// Lifetime
        /// This controls the particle's lifetime
        /// </summary>
        public float Lifetime
        {
            get { return _lifetime; }
            set
            {
                _lifetime = value;
                OnPropertyChanged("Lifetime");
            }
        }

        /// <summary>
        /// Lifetime Variance
        /// This controls the variation in the particle's lifetime
        /// </summary>
        public float LifetimeVariance
        {
            get { return _lifetimeVariance; }
            set
            {
                _lifetimeVariance = value;
                OnPropertyChanged("LifetimeVariance");
            }
        }

        /// <summary>
        /// Spin Velocity
        /// This controls the initial spin velocity over the life of the particle
        /// </summary>
        public Vector2 SpinVelocity
        {
            get { return _spinVelocity; }
            set
            {
                _spinVelocity = value;
                OnPropertyChanged("SpinVelocity");
            }
        }

        /// <summary>
        /// Spin Velocity Variance
        /// This controls the random variation of the initial spin velocity of the particle
        /// </summary>
        public float SpinVelocityVariance
        {
            get { return _spinVelocityVariance; }
            set
            {
                _spinVelocityVariance = value;
                OnPropertyChanged("SpinVelocityVariance");
            }
        }

        /// <summary>
        /// Size Range
        /// This controls the range in size of the particle over the course of its lifetime
        /// </summary>
        public Vector2 SizeRange
        {
            get { return _sizeRange; }
            set
            {
                _sizeRange = value;
                OnPropertyChanged("SizeRange");
            }
        }

        /// <summary>
        /// This controls how much particle size will vary between particles
        /// </summary>
        public float SizeVariance
        {
            get { return _sizeVariance; }
            set
            {
                _sizeVariance = value;
                OnPropertyChanged("SizeVariance");
            }
        }

        [XmlElement(ElementName = "ColorRange")]
        public Range<Color> ColorRange
        {
            get { return _colorRange; }
            set
            {
                _colorRange = value;
                OnPropertyChanged("ColorRange");
            }
        }

        /// <summary>
        /// This controls how much particle color will vary between particles
        /// </summary>
        public float ColorVariance
        {
            get { return _colorVariance; }
            set
            {
                _colorVariance = value;
                OnPropertyChanged("ColorVariance");
            }
        }

        protected void OnPropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
