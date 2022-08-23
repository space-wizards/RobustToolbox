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
        ///     Note: In the presence of tile aliases, this[A].ID does not necessarily equal A.
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

        /// <summary>
        ///     Register a tile alias with this manager.
        ///     The tile need not exist yet - the alias's creation will be deferred until it exists.
        ///     Tile aliases do not have IDs of their own and do not show up in enumeration.
        ///     Their main utility is for easier map migration.
        /// </summary>
        /// <param name="src">The source tile (i.e. name of the alias).</param>
        /// <param name="dst">The destination tile (i.e. the actual concrete tile).</param>
        void AssignAlias(string src, string dst);
    }
}
