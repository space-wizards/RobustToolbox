using System.Collections.Generic;
using Robust.Shared.IoC;

namespace Robust.Shared.Physics
{
    internal interface IContactManager
    {
        HashSet<Contact> ActiveContacts { get; }

        void FindNewContacts();
    }

    internal sealed class ContactManager
    {
        [Dependency] private readonly IBroadPhase _broadPhase = default!;

        internal PhysicsMapCallback.BroadphaseDelegate OnBroadphaseCollision = default!;

        public HashSet<Contact> ActiveContacts { get; } = new HashSet<Contact>();

        internal void FindNewContacts()
        {
            _broadPhase.UpdatePairs(OnBroadphaseCollision);
        }
    }
}
