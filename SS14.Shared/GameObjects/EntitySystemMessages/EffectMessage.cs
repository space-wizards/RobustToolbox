using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameObjects.EntitySystemMessages
{
    [Serializable, NetSerializable]
    public class EffectSystemMessage : EntitySystemMessage
    {
        /// <summary>
        /// Name of the sprite to be used for the effect
        /// </summary>
        public string EffectSprite { get; set; } = "";

        /// <summary>
        /// Effect position relative to the emit position
        /// </summary>
        public GridLocalCoordinates Coordinates { get; set; }

        /// <summary>
        /// Where the emitter was when the effect was first emitted
        /// </summary>
        public GridLocalCoordinates EmitterCoordinates { get; set; }

        /// <summary>
        /// Effect's x/y velocity
        /// </summary>
        public Vector2 Velocity { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// Effect's x/y acceleration
        /// </summary>
        public Vector2 Acceleration { get; set; } = new Vector2(0, 0);

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
        public Vector2 Size { get; set; } = new Vector2(1f, 1f);

        /// <summary>
        /// Rate of change of effect's size change
        /// </summary>
        public float SizeDelta { get; set; } = 0f;

        /// <summary>
        /// Effect's current color
        /// </summary>
        public Vector4 Color { get; set; } = new Vector4(1, 0, 0, 0);

        /// <summary>
        /// Rate of change of effect's color
        /// </summary>
        public Vector4 ColorDelta { get; set; } = new Vector4(-1, 0, 0, 0);

        /// <summary>
        ///     True if the effect is affected by lighting.
        /// </summary>
        public bool Shaded { get; set; } = true;
    }
}
