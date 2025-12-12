using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization
{
    /// <summary>
    ///     Allows a type with an argument-free constructor (i.e. <c>new()</c>) to provide its own serializer and
    ///     deserializer in place from a string, without deferring to an <see cref="ITypeSerializer{TType,TNode}"/>.
    ///     <br/>
    ///     This is much more limited than a full serializer, and only allows working with a scalar, but may be
    ///     convenient.
    /// </summary>
    public interface ISelfSerialize
    {
        /// <summary>
        ///     Deserialize the type from a given string value, after having already constructed one with <c>new()</c>.
        /// </summary>
        /// <param name="value">The scalar to deserialize from.</param>
        void Deserialize(string value);

        /// <summary>
        ///     Serialize the type to a yaml scalar (i.e. string).
        /// </summary>
        /// <returns>The serialized representation of the data.</returns>
        string Serialize();
    }
}
