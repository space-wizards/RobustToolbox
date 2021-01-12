using Robust.Shared.GameObjects.Components;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactEdge
    {
        public Contact Contact { get; set; }

        // Subject to change
        public PhysicsComponent Other => Contact.Manifold.B;

        public ContactEdge(Contact contact)
        {
            Contact = contact;
        }
    }
}
