using SS14.Shared.IoC;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Prototypes
{
    /// <summary>
    /// An IPrototype is a prototype that can be loaded from the global YAML prototypes.
    /// </summary>
    /// <remarks>
    /// To use this, the prototype must be accessible through IoC with <see cref="IoCTargetAttribute"/>
    /// and it must have a <see cref="PrototypeAttribute"/> to give it a type string.
    /// </remarks>
    public interface IPrototype : IIoCInterface
    {
        /// <summary>
        /// Load data from the YAML mappings in the prototype files.
        /// </summary>
        void LoadFrom(YamlMappingNode node);
    }

    /// <summary>
    /// Extension on <see cref="IPrototype"/> that allows it to be "indexed" by a string ID.
    /// </summary>
    public interface IIndexedPrototype : IPrototype
    {
        /// <summary>
        /// An ID for this prototype instance.
        /// If this is a duplicate, an error will be thrown.
        /// </summary>
        string ID { get; }
    }

    /// <summary>
    /// Extension on <see cref="IPrototype"/> that allows "syncing" between prototypes after all prototypes have done initial loading.
    /// To resolve reference like the entity prototype parenting.
    /// </summary>
    public interface ISyncingPrototype
    {
        void Sync(IPrototypeManager manager);
    }
}
