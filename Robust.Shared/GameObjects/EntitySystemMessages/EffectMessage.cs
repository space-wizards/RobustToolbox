using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.GameObjects.EntitySystemMessages
{
    [Serializable, NetSerializable]
    public class EffectSystemMessage : EntitySystemMessage
    {
        /// <summary>
        ///     Path to the texture used for the effect.
        ///     Can also be a path to an RSI. In that case, <see cref="RsiState"/> must also be set.
        /// </summary>
        public string EffectSprite { get; set; } = "";

        /// <summary>
        ///     Specifies the name of the RSI state to use if <see cref="EffectSprite"/> is an RSI.
        /// </summary>
        public string? RsiState { get; set; }

        /// <summary>
        ///     If the sprite is an RSI state, controls whether the animation loops or ends on the last frame.
        /// </summary>
        public bool AnimationLoops { get; set; }

        /// <summary>
        /// Effect position attached to an entity
        /// </summary>
        public EntityUid? AttachedEntityUid { get; set; }
        
        /// <summary>
        /// Effect offset relative to the parent
        /// </summary>
        public Vector2 AttachedOffset { get; set; } = Vector2.Zero;
        
        /// <summary>
        /// Effect position relative to the emit position
        /// </summary>
        public EntityCoordinates Coordinates { get; set; }

        /// <summary>
        /// Where the emitter was when the effect was first emitted
        /// </summary>
        public EntityCoordinates EmitterCoordinates { get; set; }

        /// <summary>
        /// Effect's x/y velocity
        /// </summary>
        public Vector2 Velocity { get; set; } = new(0, 0);

        /// <summary>
        /// Effect's x/y acceleration
        /// </summary>
        public Vector2 Acceleration { get; set; } = new(0, 0);

        /// <summary>
        /// Effect's radial velocity - relative to EmitterPosition
        /// </summary>
        public float RadialVelocity { get; set; } = 0f;

        /// <summary>
        /// Effect's radial acceleration
        /// </summary>
        public float RadialAcceleration { get; set; } = 0f;

        /// <summary>
        /// Effect's tangential velocity - relative to EmitterPosition
        /// </summary>
        public float TangentialVelocity { get; set; } = 0f;

        /// <summary>
        /// Effect's tangential acceleration
        /// </summary>
        public float TangentialAcceleration { get; set; } = 0f;

        /// <summary>
        /// Effect's age -- from 0f
        /// </summary>
        public TimeSpan Born { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Time after which the particle will "die"
        /// </summary>
        public TimeSpan DeathTime { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Effect's spin about its center in radians
        /// </summary>
        public float Rotation { get; set; } = 0f;

        /// <summary>
        /// Rate of change of effect's spin, radians/s
        /// </summary>
        public float RotationRate { get; set; } = 0f;

        /// <summary>
        /// Effect's current size
        /// </summary>
        public Vector2 Size { get; set; } = new(1f, 1f);

        /// <summary>
        /// Rate of change of effect's size change
        /// </summary>
        public float SizeDelta { get; set; } = 0f;

        /// <summary>
        /// Effect's current color
        /// </summary>
        public Vector4 Color { get; set; } = new(1, 0, 0, 0);

        /// <summary>
        /// Rate of change of effect's color
        /// </summary>
        public Vector4 ColorDelta { get; set; } = new(-1, 0, 0, 0);

        /// <summary>
        ///     True if the effect is affected by lighting.
        /// </summary>
        public bool Shaded { get; set; } = true;
    }
}
