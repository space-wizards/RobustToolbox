using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Random;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     This manages tile definitions for grid tiles.
    /// </summary>
    public interface ITileDefinitionManager : IEnumerable<ITileDefinition>
    {
        Tile GetVariantTile(string name, IRobustRandom random);

        Tile GetVariantTile(string name, System.Random random);

        Tile GetVariantTile(ITileDefinition tileDef, IRobustRandom random);

        Tile GetVariantTile(ITileDefinition tileDef, System.Random random);

        /// <summary>
        ///     Indexer to retrieve a tile definition by name.
        ///     Note: In the presence of tile aliases, this[A].ID does not necessarily equal A.
        /// </summary>
        /// <param name="name">The name of the tile definition.</param>
        /// <returns>The named tile definition.</returns>
        /// <seealso cref="TryGetDefinition(string,out Robust.Shared.Map.ITileDefinition?)"/>
        ITileDefinition this[string name] { get; }

        /// <summary>
        ///     Indexer to retrieve a tile definition by internal ID.
        /// </summary>
        /// <param name="id">The ID of the tile definition.</param>
        /// <returns>The tile definition.</returns>
        /// <seealso cref="TryGetDefinition(int,out Robust.Shared.Map.ITileDefinition?)"/>
        ITileDefinition this[int id] { get; }

        /// <summary>
        /// Try to retrieve a tile definition by name.
        /// </summary>
        /// <remarks>
        /// Note: In the presence of tile aliases, this[A].ID does not necessarily equal A.
        /// </remarks>
        /// <param name="name">The name of the tile definition to look up.</param>
        /// <param name="definition">The found tile definition, if it exists.</param>
        /// <returns>True if a tile definition was resolved, false otherwise.</returns>
        /// <seealso cref="this[string]"/>
        bool TryGetDefinition(string name, [NotNullWhen(true)] out ITileDefinition? definition);

        /// <summary>
        /// Try to retrieve a tile definition by tile ID.
        /// </summary>
        /// <param name="id">The ID of the tile definition to look up.</param>
        /// <param name="definition">The found tile definition, if it exists.</param>
        /// <returns>True if a tile definition was resolved, false otherwise.</returns>
        /// <seealso cref="this[int]"/>
        bool TryGetDefinition(int id, [NotNullWhen(true)] out ITileDefinition? definition);

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
