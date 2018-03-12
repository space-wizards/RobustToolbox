using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    public struct RayCastResults
    {
        public bool HitObject => Distance < float.PositiveInfinity;

        public IEntity HitEntity { get; }
        public Vector2 HitPos { get; }
        public float Distance { get; }

        public RayCastResults(float distance, Vector2 hitPos, IEntity hitEntity)
        {
            Distance = distance;
            HitPos = hitPos;
            HitEntity = hitEntity;
        }
    }
}
