using System.Collections.Generic;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     This manages tile definitions for grid tiles.
    /// </summary>
    public interface ITileDefinitionManager : IEnumerable<ITileDefinition>
    {
        /// <summary>
        ///     Indexer to retrieve a tile definition by name.
        /// </summary>
        /// <param name="name">The name of the tile definition.</param>
        /// <returns>The named tile definition.</returns>
        ITileDefinition this[string name] { get; }

        /// <summary>
        ///     Indexer to retrieve a tile definition by internal ID.
        /// </summary>
        /// <param name="id">The ID of the tile definition.</param>
        /// <returns>The tile definition.</returns>
        ITileDefinition this[int id] { get; }

        /// <summary>
        ///     The number of tile definitions contained inside of this manager.
        /// </summary>
        int Count { get; }

        void Initialize();

        /// <summary>
        ///     Register a definition with this manager.
        /// </summary>
        /// <param name="tileDef">THe definition to register.</param>
        void Register(ITileDefinition tileDef);
    }
}
