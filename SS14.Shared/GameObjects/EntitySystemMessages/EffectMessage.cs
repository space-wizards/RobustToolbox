using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;

namespace SS14.Shared.GameObjects.EntitySystemMessages
{
    [Serializable]
    public class EffectSystemMessage : EntitySystemMessage
    {
        /// <summary>
        /// Name of the sprite to be used for the effect
        /// </summary>
        public string EffectSprite = "";

        /// <summary>
        /// Effect position relative to the emit position
        /// </summary>
        public LocalCoordinates Coordinates;

        /// <summary>
        /// Where the emitter was when the effect was first emitted
        /// </summary>
        public LocalCoordinates EmitterCoordinates;

        /// <summary>
        /// Effect's x/y velocity
        /// </summary>
        public Vector2 Velocity = new Vector2(0, 0);

        /// <summary>
        /// Effect's x/y acceleration
        /// </summary>
        public Vector2 Acceleration = new Vector2(0, 0);

        /// <summary>
        /// Effect's radial velocity - relative to EmitterPosition
        /// </summary>
        public float RadialVelocity = 0f;

        /// <summary>
        /// Effect's radial acceleration
        /// </summary>
        public float RadialAcceleration = 0f;

        /// <summary>
        /// Effect's tangential velocity - relative to EmitterPosition
        /// </summary>
        public float TangentialVelocity = 0f;

        /// <summary>
        /// Effect's tangential acceleration
        /// </summary>
        public float TangentialAcceleration = 0f;

        /// <summary>
        /// Effect's age -- from 0f
        /// </summary>
        public TimeSpan Born = TimeSpan.Zero;

        /// <summary>
        /// Time after which the particle will "die"
        /// </summary>
        public TimeSpan DeathTime = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Effect's spin about its center in radians
        /// </summary>
        public float Rotation = 0f;

        /// <summary>
        /// Rate of change of effect's spin, radians/s
        /// </summary>
        public float RotationRate = 0f;

        /// <summary>
        /// Effect's current size
        /// </summary>
        public Vector2 Size = new Vector2(1f, 1f);

        /// <summary>
        /// Rate of change of effect's size change
        /// </summary>
        public float SizeDelta = 0f;

        /// <summary>
        /// Effect's current color
        /// </summary>
        public Vector4 Color = new Vector4(1, 0, 0, 0);

        /// <summary>
        /// Rate of change of effect's color
        /// </summary>
        public Vector4 ColorDelta = new Vector4(-1, 0, 0, 0);

        /// <summary>
        ///     True if the effect is affected by lighting.
        /// </summary>
        public bool Shaded = true;
    }
}
