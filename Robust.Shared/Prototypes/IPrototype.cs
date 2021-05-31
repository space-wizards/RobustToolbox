using Robust.Shared.Serialization.Manager.Attributes.Deserializer;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    ///     An IPrototype is a prototype that can be loaded from the global YAML prototypes.
    /// </summary>
    /// <remarks>
    ///     To use this, the prototype must be accessible through IoC with <see cref="IoCTargetAttribute"/>
    ///     and it must have a <see cref="PrototypeAttribute"/> to give it a type string.
    /// </remarks>
    [DataFieldDeserializer(typeof(PrototypeDataFieldDeserializer))]
    public interface IPrototype
    {
        /// <summary>
        /// An ID for this prototype instance.
        /// If this is a duplicate, an error will be thrown.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)] string ID { get; }
    }

    public interface IInheritingPrototype
    {
        string? Parent { get; }

        bool Abstract { get; }
    }
}
