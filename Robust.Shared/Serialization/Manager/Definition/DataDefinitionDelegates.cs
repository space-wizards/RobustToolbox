using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager.Definition;

[Obsolete("Used only in source generation")]
public delegate void ValidateAllFieldsDelegate(
    Dictionary<string, ValidationNode> nodes,
    MappingDataNode node,
    ISerializationManager serialization,
    ISerializationContext? context = null
);
