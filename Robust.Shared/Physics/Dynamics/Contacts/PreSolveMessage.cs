using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class PreSolveMessage : EntitySystemMessage
    {
        public Contact Contact { get; }

        public Collision.Manifold OldManifold { get; }

        public PreSolveMessage(Contact contact, Collision.Manifold oldManifold)
        {
            Contact = contact;
            OldManifold = oldManifold;
        }
    }
}
