using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    ///     An IPrototype is a prototype that can be loaded from the global YAML prototypes.
    /// </summary>
    /// <remarks>
    ///     To use this, the prototype must be accessible through IoC with <see cref="IoCTargetAttribute"/>
    ///     and it must have a <see cref="PrototypeAttribute"/> to give it a type string.
    /// </remarks>
    public interface IPrototype
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
        void Reset();

        /// <summary>
        /// Sync and update cross-referencing data.
        /// Syncing works in stages, each time it will be called with the stage it's currently on.
        /// Each prototype will be called in a stage, then the stage count goes up.
        /// </summary>
        /// <remarks>
        /// The order of syncing is in no way guaranteed to be consistent across stages.
        /// This means that on stage 1 prototype A might sync first, but on stage 2 prototype B might.
        /// </remarks>
        /// <param name="prototypes"></param>
        /// <param name="serialization"></param>
        /// <param name="stage">The current sync stage.</param>
        /// <param name="result"></param>
        /// <returns>Whether or not the prototype will be included in the next sync stage</returns>
        bool Sync(IPrototypeManager prototypes, ISerializationManager serialization, int stage, DeserializationResult result);
    }
}
