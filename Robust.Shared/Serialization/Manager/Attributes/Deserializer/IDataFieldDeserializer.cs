using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager.Attributes.Deserializer
{
    public interface IDataFieldDeserializer
    {
        /// <summary>
        ///     Reads a value created from <see cref="node"/> into the field
        ///     <see cref="field"/> of type <see cref="type"/> that belongs
        ///     to the object <see cref="obj"/>.
        /// </summary>
        /// <param name="obj">The object that the field belongs to.</param>
        /// <param name="type">The type of the field.</param>
        /// <param name="node">The node to deserialize into the field.</param>
        /// <param name="manager">The manager doing the deserialization.</param>
        /// <param name="dependencies">The dependencies used by the manager.</param>
        /// <param name="context">The deserialization context.</param>
        /// <param name="skipHook">Whether or not to skip serialization hooks.</param>
        /// <param name="field">The field that is being assigned to.</param>
        /// <returns>The deserialized result.</returns>
        DeserializationResult Read(
            object obj,
            Type type,
            DataNode node,
            ISerializationManager manager,
            IDependencyCollection dependencies,
            ISerializationContext? context,
            bool skipHook,
            FieldDefinition field);
    }
}
