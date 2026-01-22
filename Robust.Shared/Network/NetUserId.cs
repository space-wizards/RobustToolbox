using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network
{
    /// <summary>
    ///     A unique identifier for a given user's account.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     If connected to auth and auth is mandatory, this is guaranteed to be
    ///     <b>globally unique</b> within that auth service, duplicate players will not exist.
    /// </para>
    /// <para>
    ///     Similarly, the engine assumes that if the user is <see cref="LoginType.GuestAssigned">a known guest with an
    ///     assigned ID</see>, their ID is also globally unique.
    /// </para>
    /// <para>
    ///     This is independent of username, and in the current auth implementation users can freely change username.
    ///     Think of this as a way to refer to a given account regardless of what it's named.
    /// </para>
    /// </remarks>
    [Serializable, NetSerializable]
    public struct NetUserId : IEquatable<NetUserId>, ISelfSerialize
    {
        public readonly Guid UserId;

        public NetUserId(Guid userId)
        {
            UserId = userId;
        }

        public override bool Equals(object? obj) =>
        obj switch {
            Guid id => Equals(id),
            NetUserId id => Equals(id),
            _ => false,
        };

        public bool Equals(NetUserId other) => UserId == other.UserId;

        public override int GetHashCode() => UserId.GetHashCode();

        public override string ToString() => UserId.ToString();

        public static bool operator ==(NetUserId id1, NetUserId id2) => id1.Equals(id2);

        public static bool operator !=(NetUserId id1, NetUserId id2) => !(id1 == id2);

        public static implicit operator Guid(NetUserId id) => id.UserId;
        public static explicit operator NetUserId(Guid id) => new(id);

        void ISelfSerialize.Deserialize(string value)
        {
            this = (NetUserId) Guid.Parse(value);
        }

        string ISelfSerialize.Serialize()
        {
            return ToString();
        }
    }
}
