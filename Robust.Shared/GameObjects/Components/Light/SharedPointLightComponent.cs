using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Robust.Shared.GameObjects
{
    [NetworkedComponent]
    public abstract class SharedPointLightComponent : Component
    {
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

        [DataField("color")]
        protected Color _color = Color.White;

        /// <summary>
        /// Offset from the center of the entity.
        /// </summary>
        [DataField("offset")]
        protected Vector2 _offset = Vector2.Zero;

        [DataField("energy")]
        private float _energy = 1f;
        [DataField("softness")]
        private float _softness = 1f;

        /// <summary>
        ///     Whether this pointlight should cast shadows
        /// </summary>
        [DataField("castShadows")]
        public bool CastShadows = true;

        [Access(typeof(SharedPointLightSystem))]
        [DataField("enabled")]
        public bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable] // please somebody ECS animations
        public bool Enabled
        {
            get => _enabled;
            [Obsolete("Use the system's setter")]
            set => _sysMan.GetEntitySystem<SharedPointLightSystem>().SetEnabled(Owner, value, this);
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Color Color
        {
            get => _color;
            set
            {
                if (_color.Equals(value)) return;
                _color = value;
                Dirty();
            }
        }

        /// <summary>
        /// How far the light projects.
        /// </summary>
        [DataField("radius")]
        [Access(typeof(SharedPointLightSystem))]
        public float _radius = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable] // please somebody ECS animations
        public float Radius
        {
            get => _radius;
            [Obsolete("Use the system's setter")]
            set => _sysMan.GetEntitySystem<SharedPointLightSystem>().SetRadius(Owner, value, this);
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                if (_offset.EqualsApprox(value)) return;
                _offset = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public float Energy
        {
            get => _energy;
            set
            {
                if (_energy.Equals(value)) return;
                _energy = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Soft shadow strength multiplier.
        ///     Has no effect if soft shadows are not enabled.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public float Softness
        {
            get => _softness;
            set
            {
                if (_softness.Equals(value)) return;
                _softness = value;
                Dirty();
            }
        }
    }

    public sealed class PointLightToggleEvent : EntityEventArgs
    {
        public bool Enabled;

        public PointLightToggleEvent(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
