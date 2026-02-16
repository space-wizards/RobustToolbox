using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager.Definition;

[Obsolete("Used only in source generation")]
public delegate void PopulateDelegateSignature<T>(
    ref T target,
    MappingDataNode mappingDataNode,
    ISerializationManager serialization,
    SerializationHookContext hookCtx,
    ISerializationContext? context
);

[Obsolete("Used only in source generation")]
public delegate void SerializeDelegateSignature<T>(
    T obj,
    MappingDataNode mapping,
    ISerializationManager serialization,
    ISerializationContext? context,
    bool alwaysWrite,
    ImmutableDictionary<string, object?> defaultValues
);

[Obsolete("Used only in source generation")]
public delegate void ValidateAllFieldsDelegate(
    Dictionary<string, ValidationNode> nodes,
    MappingDataNode node,
    ISerializationManager serialization,
    ISerializationContext? context = null
);
