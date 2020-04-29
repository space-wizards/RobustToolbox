using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public class VirtualForce
    {
        private Vector2 _force;

        public Vector2 Force
        {
            get => _force;
            set => _force = value;
        }

        public VirtualForce(Vector2 force)
        {
            _force = force;
        }

        public Vector2 GetImpulse(float frameTime)
        {
            return Force * frameTime;
        }
    }
}
