using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Robust.Shared.EntitySerialization;

/// <summary>
/// Class containing information about entities that were loaded from a yaml file.
/// </summary>
public sealed class LoadResult
{
    /// <summary>
    /// The file format version.
    /// </summary>
    public int Version;

    /// <summary>
    /// The category of the file that was loaded in.
    /// This might not match the actual final result. E.g., when loading in a grid file, a map may automatically gets
    /// generated for it via <see cref="EntityDeserializer.AdoptGrids"/>.
    /// </summary>
    public FileCategory Category = FileCategory.Unknown;

    /// <summary>
    /// The engine version that was used to write the file. See <see cref="CVars.BuildEngineVersion"/>.
    /// </summary>
    public string? EngineVersion;

    /// <summary>
    /// The fork that was used to write the file. See <see cref="CVars.BuildForkId"/>.
    /// </summary>
    public string? ForkId;

    /// <summary>
    /// The fork version that was used to write the file. See <see cref="CVars.BuildVersion"/>.
    /// </summary>
    public string? ForkVersion;

    /// <summary>
    /// The <see cref="DateTime.UtcNow"/> when the file was created.
    /// </summary>
    public DateTime? Time;

    /// <summary>
    /// Set of all entities that were created while the file was being loaded.
    /// </summary>
    public readonly HashSet<EntityUid> Entities = new();

    /// <summary>
    /// Set of entities that are not parented to other entities. This will be a combination of <see cref="Maps"/>,
    /// <see cref="Orphans"/>, and <see cref="NullspaceEntities"/>.
    /// </summary>
    public readonly HashSet<EntityUid> RootNodes = new();

    public readonly HashSet<Entity<MapComponent>> Maps = new();

    public readonly HashSet<Entity<MapGridComponent>> Grids = new();

    /// <summary>
    /// Deserialized entities that need to be assigned a new parent. These differ from "true" null-space entities.
    /// E,g, saving a grid without saving the map would make the grid an "orphan".
    /// </summary>
    public readonly HashSet<EntityUid> Orphans = new();

    /// <summary>
    /// List of null-space entities. This contains all entities without a parent that don't have a
    /// <see cref="MapComponent"/>, and were not listed as orphans
    /// </summary>
    public readonly HashSet<EntityUid> NullspaceEntities = new();
}
