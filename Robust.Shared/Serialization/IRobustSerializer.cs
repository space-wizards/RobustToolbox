using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Robust.Shared.Serialization
{
    [NotContentImplementable]
    public interface IRobustSerializer
    {
        /// <summary>
        /// Specifies how the serializer should handle read floating point values.
        /// </summary>
        /// <remarks>
        /// Both sides of the network need not have the same float handling flags.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if set after the serializer has already been initialized.
        /// (must be done from <see cref="ModRunLevel.PreInit"/>)
        /// </exception>
        SerializerFloatFlags FloatFlags { get; set; }

        /// <exception cref="InvalidOperationException">
        /// Thrown if called twice.
        /// </exception>
        void Initialize();
        void Serialize(Stream stream, object toSerialize);

        /// <summary>
        /// Serializes an object with exact known type to skip a type ID header.
        /// </summary>
        /// <remarks>
        /// This is more efficient than <see cref="Serialize"/> because it does not use a few bytes on type ID header.
        /// Output from this method is not compatible with <see cref="Deserialize"/> or vice versa.
        /// The type argument of the <see cref="DeserializeDirect{T}"/> call must also match, obviously.
        /// </remarks>
        /// <param name="stream">The stream to write into.</param>
        /// <param name="toSerialize">
        /// The object to serialize.
        /// The object MUST have the exact type of <typeparamref name="T"/>
        /// </param>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        void SerializeDirect<T>(Stream stream, T toSerialize);
        T Deserialize<T>(Stream stream);

        /// <summary>
        /// Deserialize side of <see cref="DeserializeDirect{T}"/>.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="value">Value that was deserialized.</param>
        /// <typeparam name="T">Exact type of object to deserialize.</typeparam>
        void DeserializeDirect<T>(Stream stream, out T value);
        object Deserialize(Stream stream);
        bool CanSerialize(Type type);

        /// <summary>
        /// Searches for a type with a given SerializedName that can be assigned to another type.
        /// </summary>
        /// <param name="assignableType">object type that it can be assigned to, like a base class or interface.</param>
        /// <param name="serializedTypeName">The serializedName inside the <see cref="NetSerializableAttribute"/>.</param>
        /// <returns>Type found, if any.</returns>
        Type? FindSerializedType(Type assignableType, string serializedTypeName);

        Task Handshake(INetChannel sender);

        event Action ClientHandshakeComplete;

        byte[] GetSerializableTypesHash();
        string GetSerializableTypesHashString();

        (byte[] Hash, byte[] Package) GetStringSerializerPackage();
    }

    internal interface IRobustSerializerInternal : IRobustSerializer
    {
        Dictionary<Type, uint> GetTypeMap();

        long LargestObjectSerializedBytes { get; }
        Type? LargestObjectSerializedType { get; }
        long BytesSerialized { get; }
        long ObjectsSerialized { get; }
        long LargestObjectDeserializedBytes { get; }
        Type? LargestObjectDeserializedType { get; }
        long BytesDeserialized { get; }
        long ObjectsDeserialized { get; }
    }

    /// <summary>
    /// Flags for <see cref="IRobustSerializer"/> float handling.
    /// </summary>
    /// <remarks>
    /// These flags have no effect on values passed in a <see cref="UnsafeFloat"/>, <see cref="UnsafeHalf"/> or
    /// <see cref="UnsafeDouble"/>.
    /// </remarks>
    [Flags]
    public enum SerializerFloatFlags
    {
        /// <summary>
        /// No special behavior: floating point values are read exactly as sent over the network.
        /// </summary>
        None = 0,

        /// <summary>
        /// Read NaN values will be cleared to zero.
        /// </summary>
        RemoveReadNan = 1 << 0,
    }
}
