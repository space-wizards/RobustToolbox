using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Prototype that represents an alias from one tile ID to another.
/// Tile alias prototypes, unlike tile prototypes, are implemented here, as they're really just fed to TileDefinitionManager.
/// </summary>
[Prototype("tileAlias", -1)]
public sealed class TileAliasPrototype : IPrototype
{
    /// <summary>
    /// The target tile ID to alias to.
    /// </summary>
    [ViewVariables]
    [DataField("target")]
    public string Target { get; private set; } = default!;

    /// <summary>
    /// The source tile ID (and the ID of this tile alias).
    /// </summary>
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; private set; } = default!;
}
