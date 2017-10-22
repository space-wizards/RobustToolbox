using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Maths;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    public class BumpEventArgs : EventArgs
    {
        public readonly IEntity Bumper;
        public readonly IEntity Bumping;

        public BumpEventArgs(IEntity bumper, IEntity bumping)
        {
            Bumper = bumper;
            Bumping = bumping;
        }
    }

    public interface ICollidableComponent : IComponent, ICollidable
    {
        event EventHandler<BumpEventArgs> OnBumped;
        bool TryCollision(Vector2 offset, bool bump = false);
    }
}
