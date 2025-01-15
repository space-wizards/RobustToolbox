using System.Collections.Generic;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;

namespace Robust.Shared.EntitySerialization.Components;

/// <summary>
/// Used by <see cref="MapLoaderSystem"/> to track the original tile map from when a map was loaded.
/// </summary>
/// <remarks>
/// <para>
/// This component is used to reduce differences on map saving, by making it so that a tile map can be re-used between map saves even if internal engine IDs change.
/// </para>
/// <para>
/// This component is created on every grid entity read during map load.
/// This means loading a multi-grid map will create multiple of these components.
/// When re-saving the map, the map loader will arbitrarily choose which available <see cref="MapSaveTileMapComponent"/>
/// to use.
/// </para>
/// </remarks>
[RegisterComponent, UnsavedComponent]
internal sealed partial class MapSaveTileMapComponent : Component
{
    public Dictionary<int, string> TileMap = [];
}
