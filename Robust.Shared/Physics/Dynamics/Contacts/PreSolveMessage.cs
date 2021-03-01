using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class PreSolveMessage : EntitySystemMessage
    {
        public Contact Contact { get; }

        public Manifold OldManifold { get; }

        public PreSolveMessage(Contact contact, Manifold oldManifold)
        {
            Contact = contact;
            OldManifold = oldManifold;
        }
    }
}
