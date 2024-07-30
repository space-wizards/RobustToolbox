using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;



using Robust.Shared.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
///     The appearance component allows game logic to be more detached from the actual visuals of an entity such as 2D
///     sprites, 3D, particles, lights...
///     It does this with a "data" system. Basically, code writes data to the component, and the component will use
///     prototype-based configuration to change the actual visuals.
///     The data works using a simple key/value system. It is recommended to use enum keys to prevent errors.
///     Visualization works client side with derivatives of the <see cref="Robust.Client.GameObjects.VisualizerSystem">VisualizerSystem</see> class and corresponding components.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AppearanceComponent : Component
{
    /// <summary>
    /// Whether or not the appearance needs to be updated.
    /// </summary>
    [ViewVariables] internal bool AppearanceDirty;

    /// <summary>
    /// If true, this entity will have its appearance updated in the next frame update.
    /// </summary>
    /// <remarks>
    /// If an entity is outside of PVS range, this may be false while <see cref="AppearanceDirty"/> is true.
    /// </remarks>
    [ViewVariables] internal bool UpdateQueued;

    [DataField(customTypeSerializer: typeof(AppearanceSerializer))]
    public Dictionary<Enum, object> AppearanceData = new();

    [Obsolete("Use SharedAppearanceSystem instead")]
    public bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data)
    {
        if (AppearanceData.TryGetValue(key, out var dat) && dat is T)
        {
            data = (T)dat;
            return true;
        }

        data = default!;
        return false;
    }

    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class AppearanceDataDummy
    {
        public Dictionary<Enum, object> AppearanceData;
    }
}

public sealed class AppearanceSerializer : ITypeReader<Dictionary<Enum, object>, MappingDataNode>, ITypeCopier<Dictionary<Enum, object>>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var validated = new List<ValidationNode>();

        if (node.Count <= 0)
            return new ValidatedSequenceNode(validated);

        var reflection = dependencies.Resolve<IReflectionManager>();

        foreach (var data in node)
        {
            var key = data.Key.ToYamlNode().AsString();

            if (data.Value.Tag == null)
            {
                validated.Add(new ErrorNode(data.Key, $"Unable to validate {key}'s type"));
                continue;
            }

            var typeString = data.Value.Tag[6..];

            if (!reflection.TryLooseGetType(typeString, out var type))
            {
                validated.Add(new ErrorNode(data.Key, $"Unable to find type for {typeString}"));
                continue;
            }

            var validatedNode = serializationManager.ValidateNode(type, data.Value, context);
            validated.Add(validatedNode);
        }

        return new ValidatedSequenceNode(validated);
    }

    public Dictionary<Enum, object> Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<Enum, object>>? instanceProvider = null)
    {
        var value = instanceProvider != null ? instanceProvider() : new Dictionary<Enum, object>();

        if (node.Count <= 0)
            return value;

        var reflection = dependencies.Resolve<IReflectionManager>();

        foreach (var data in node)
        {
            var key = data.Key.ToYamlNode().ToString();

            if (!serializationManager.ReflectionManager.TryParseEnumReference(key, out var @enum))
                throw new ArgumentException($"Failed to parse enum {key}");

            if (data.Value.Tag == null)
                throw new NullReferenceException($"Found null tag for {key}");

            var typeString = data.Value.Tag[6..];

            if (!reflection.TryLooseGetType(typeString, out var type))
                throw new NullReferenceException($"Found null type for {key}");

            var bbData = serializationManager.Read(type, data.Value, hookCtx, context);

            if (bbData == null)
                throw new NullReferenceException($"Found null data for {key}, expected {type}");

            value[@enum] = bbData;
        }

        return value;
    }

    public void CopyTo(
        ISerializationManager serializationManager,
        Dictionary<Enum, object> source,
        ref Dictionary<Enum, object> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target.Clear();
        using var enumerator = source.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            target[current.Key] = current.Value;
        }
    }
}
