using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Physics;

/// <summary>
/// Special-case to avoid writing grid fixtures.
/// </summary>
public sealed class FixtureSerializer : ITypeSerializer<Dictionary<string, Fixture>, MappingDataNode>, ITypeCopier<Dictionary<string, Fixture>>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var seq = new List<ValidationNode>(node.Count);
        var keys = new HashSet<string>();

        foreach (var subNode in node)
        {
            var key = (ValueDataNode)subNode.Key;

            if (!keys.Add(key.Value))
            {
                seq.Add(new ErrorNode(subNode.Key, $"Found duplicate fixture ID {key.Value}"));
                continue;
            }

            seq.Add(serializationManager.ValidateNode<Fixture>(subNode.Value, context));
        }

        return new ValidatedSequenceNode(seq);
    }

    public Dictionary<string, Fixture> Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Dictionary<string, Fixture>>? instantiation = default)
    {
        var value = instantiation != null ? instantiation() : new Dictionary<string, Fixture>(node.Count);

        foreach (var subNode in node)
        {
            var key = (ValueDataNode)subNode.Key;

            var fixture = serializationManager.Read<Fixture>(subNode.Value, hookCtx, context, notNullableOverride: true);
            fixture.ID = key.Value;
            value.Add(key.Value, fixture);
        }

        return value;
    }

    public void CopyTo(ISerializationManager serializationManager, Dictionary<string, Fixture> source, ref Dictionary<string, Fixture> target,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        target.Clear();

        foreach (var (id, fixture) in source)
        {
            var newFixture = serializationManager.CreateCopy(fixture, hookCtx, context);
            newFixture.ID = id;

            target.Add(id, newFixture);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<string, Fixture> value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var seq = new MappingDataNode();

        if (value.Count == 0)
            return seq;

        if (context is MapSerializationContext mapContext)
        {
            // Don't serialize mapgrid fixtures because it's bloat and we'll just generate them at runtime.
            if (dependencies.Resolve<IEntityManager>().HasComponent<MapGridComponent>(mapContext.CurrentWritingEntity))
            {
                return seq;
            }
        }

        foreach (var (id, fixture) in value)
        {
            seq.Add(id, serializationManager.WriteValue(fixture, alwaysWrite, context, notNullableOverride: true));
        }

        return seq;
    }
}
