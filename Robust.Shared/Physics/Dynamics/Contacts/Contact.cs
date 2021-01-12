using Robust.Shared.Interfaces.Physics;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class Contact
    {
        // This will likely change a lot in future.
        public Manifold Manifold { get; private set; }

        /// <summary>
        ///     Has this contact already been added to an island?
        /// </summary>
        public bool IslandFlag { get; set; }

        public Contact(Manifold manifold)
        {
            Manifold = manifold;
        }
    }
}
