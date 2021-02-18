using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public readonly struct RayCastResults
    {

        /// <summary>
        ///     The entity that was hit. <see langword="null" /> if no entity was hit.
        /// </summary>
        public IEntity HitEntity { get; }

        /// <summary>
        ///     The point of contact where the entity was hit. Defaults to <see cref="Vector2.Zero"/> if no entity was hit.
        /// </summary>
        public Vector2 HitPos { get; }

        /// <summary>
        ///     The distance from point of origin to the context point. 0.0f if nothing was hit.
        /// </summary>
        public float Distance { get; }

        public RayCastResults(float distance, Vector2 hitPos, IEntity hitEntity)
        {
            Distance = distance;
            HitPos = hitPos;
            HitEntity = hitEntity;
        }
    }
}
