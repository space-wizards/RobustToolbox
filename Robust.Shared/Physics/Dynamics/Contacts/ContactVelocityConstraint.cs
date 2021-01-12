namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactVelocityConstraint
    {
        public int ContactIndex { get; set; }

        /// <summary>
        ///     Index of BodyA in the island.
        /// </summary>
        public int IndexA { get; set; }

        /// <summary>
        ///     Index of BodyB in the island.
        /// </summary>
        public int IndexB { get; set; }
    }
}
