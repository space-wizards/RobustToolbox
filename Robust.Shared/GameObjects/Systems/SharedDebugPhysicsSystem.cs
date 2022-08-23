using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Only reason this exists is so we can avoid the message allocation when not needed.
    ///     It's only useful for debugging and is kind of a perf sink.
    /// </summary>
    public abstract class SharedDebugPhysicsSystem : EntitySystem
    {
        public virtual void HandlePreSolve(Contact contact, in Manifold oldManifold) {}
    }
}
