using Robust.Shared.GameObjects.Components;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactEdge
    {
        /// <summary>
        ///     This contact in the chain.
        /// </summary>
        public Contact? Contact { get; set; } = default!;

        public ContactEdge? Next { get; set; }

        public ContactEdge? Previous { get; set; } = default!;

        // Subject to change
        public PhysicsComponent? Other { get; set; } = default!;

        public ContactEdge() {}

        public ContactEdge(Contact contact, ContactEdge previous, PhysicsComponent other)
        {
            Contact = contact;
            Previous = previous;
            Other = other;
        }
    }
}
