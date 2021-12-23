using System;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [NetworkedComponent]
    public abstract class SharedPointLightComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "PointLight";

        [DataField("enabled")]
        protected bool _enabled = true;

        [DataField("color")]
        protected Color _color = Color.White;

        /// <summary>
        /// How far the light projects.
        /// </summary>
        [DataField("radius")]
        protected float _radius = 5f;

        /// <summary>
        /// Offset from the center of the entity.
        /// </summary>
        [DataField("offset")]
        protected Vector2 _offset = Vector2.Zero;

        [DataField("energy")]
        private float _energy = 1f;
        [DataField("softness")]
        private float _softness = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                _entMan.EventBus.RaiseLocalEvent(Owner, new PointLightToggleEvent(_enabled));
                Dirty();
            }
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

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual float Radius
        {
            get => _radius;
            set
            {
                if (MathHelper.CloseToPercent(_radius, value)) return;
                _radius = MathF.Max(value, 0.01f); // setting radius to 0 causes exceptions, so just use a value close enough to zero that it's unnoticeable.
                Dirty();
            }
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
        public virtual float Energy
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
        public virtual float Softness
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
