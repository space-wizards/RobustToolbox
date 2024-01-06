using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public partial class SerializationManager
{
    private delegate DataNode PushCompositionDelegate(
        Type type,
        DataNode parent,
        DataNode child,
        ISerializationContext? context = null);

    private readonly ConcurrentDictionary<(Type value, Type node), PushCompositionDelegate> _compositionPushers = new();

    public DataNode PushComposition(Type type, DataNode[] parents, DataNode child, ISerializationContext? context = null)
    {
        DebugTools.Assert(parents.All(x => x.GetType() == child.GetType()));

        var pusher = GetOrCreatePushCompositionDelegate(type, child);

        var node = child;
        for (int i = 0; i < parents.Length; i++)
        {
            node = pusher(type, parents[i], node, context);
        }

        return node;
    }

    private PushCompositionDelegate GetOrCreatePushCompositionDelegate(Type type, DataNode node)
    {
        return _compositionPushers.GetOrAdd((type, node.GetType()), static (tuple, vfArgument) =>
        {
            var (value, nodeType) = tuple;
            var (node, instance) = vfArgument;

            var instanceConst = Expression.Constant(instance);

            var typeParam = Expression.Parameter(typeof(Type), "type");
            var parentParam = Expression.Parameter(typeof(DataNode), "parent");
            var childParam = Expression.Parameter(typeof(DataNode), "child");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            Expression expression;

            if (instance._regularSerializerProvider.TryGetTypeNodeSerializer(typeof(ITypeInheritanceHandler<,>), value, nodeType, out var handler))
            {
                var readerType = typeof(ITypeInheritanceHandler<,>).MakeGenericType(value, nodeType);
                var readerConst = Expression.Constant(handler, readerType);

                expression = Expression.Call(
                    instanceConst,
                    nameof(PushInheritance),
                    new []{value, nodeType},
                    readerConst,
                    Expression.Convert(parentParam, nodeType),
                    Expression.Convert(childParam, nodeType),
                    contextParam);
            }
            else if (nodeType == typeof(MappingDataNode) && instance.TryGetDefinition(value, out var dataDefinition))
            {
                var definitionConst = Expression.Constant(dataDefinition, typeof(DataDefinition));

                expression = Expression.Call(
                    instanceConst,
                    nameof(PushInheritanceDefinition),
                    Type.EmptyTypes,
                    Expression.Convert(childParam, nodeType),
                    Expression.Convert(parentParam, nodeType),
                    definitionConst,
                    instanceConst,
                    contextParam);
            }
            else
            {
                expression = node switch
                {
                    SequenceDataNode => Expression.Call(
                        instanceConst,
                        nameof(PushInheritanceSequence),
                        Type.EmptyTypes,
                        Expression.Convert(childParam, nodeType),
                        Expression.Convert(parentParam, nodeType)),
                    MappingDataNode => Expression.Call(
                        instanceConst,
                        nameof(PushInheritanceMapping),
                        Type.EmptyTypes,
                        Expression.Convert(childParam, nodeType),
                        Expression.Convert(parentParam, nodeType)),
                    _ => childParam
                };
            }

            return Expression.Lambda<PushCompositionDelegate>(
                expression,
                typeParam,
                parentParam,
                childParam,
                contextParam).Compile();
        }, (node, this));
    }

    private SequenceDataNode PushInheritanceSequence(SequenceDataNode child, SequenceDataNode parent)
    {
        //todo implement different inheritancebehaviours for yamlfield
        // I have NFI what this comment means.

        var result = new SequenceDataNode(child.Count + parent.Count);
        foreach (var entry in parent)
        {
            result.Add(entry);
        }
        foreach (var entry in child)
        {
            result.Add(entry);
        }

        return result;
    }

    private MappingDataNode PushInheritanceMapping(MappingDataNode child, MappingDataNode parent)
    {
        //todo implement different inheritancebehaviours for yamlfield
        // I have NFI what this comment means.

        var result = new MappingDataNode(child.Count + parent.Count);
        foreach (var (k, v) in parent)
        {
            result[k] = v;
        }
        foreach (var (k, v) in child)
        {
            result[k] = v;
        }

        return result;
    }

    private MappingDataNode PushInheritanceDefinition(MappingDataNode child, MappingDataNode parent,
        DataDefinition definition, SerializationManager serializationManager, ISerializationContext? context = null)
    {
        var newMapping = child.Copy();
        var processedTags = new HashSet<string>();
        var fieldQueue = new Queue<FieldDefinition>(definition.BaseFieldDefinitions);
        while (fieldQueue.TryDequeue(out var field))
        {
            if (field.InheritanceBehavior == InheritanceBehavior.Never) continue;

            if (field.Attribute is DataFieldAttribute dfa)
            {
                // tag is set on data definition creation
                if(!processedTags.Add(dfa.Tag!)) continue; //tag was already processed, probably because we are using the same tag in an include
                var key = new ValueDataNode(dfa.Tag);
                if (parent.TryGetValue(key, out var parentValue))
                {
                    if (newMapping.TryGetValue(key, out var childValue))
                    {
                        if (field.InheritanceBehavior == InheritanceBehavior.Always)
                        {
                            newMapping[key] = PushComposition(field.FieldType, new[] { parentValue }, childValue, context);
                        }
                    }
                    else
                    {
                        newMapping.Add(key, parentValue);
                    }
                }
            }
            else
            {
                //there is a definition garantueed to be present for this type since the fields are validated in initialize
                //therefore we can silence nullability here
                var def = serializationManager.GetDefinition(field.FieldType)!;
                foreach (var includeFieldDef in def.BaseFieldDefinitions)
                {
                    fieldQueue.Enqueue(includeFieldDef);
                }
            }
        }

        return newMapping;
    }

    public TNode PushInheritance<TType, TNode>(ITypeInheritanceHandler<TType, TNode> inheritanceHandler,
        TNode parent, TNode child,
        ISerializationContext? context = null) where TNode : DataNode
    {
        return inheritanceHandler.PushInheritance(this, child, parent, DependencyCollection, context);
    }

    public TNode PushInheritance<TType, TNode, TInheritanceHandler>(TNode parent, TNode child,
        ISerializationContext? context = null) where TNode : DataNode
        where TInheritanceHandler : ITypeInheritanceHandler<TType, TNode>
    {
        return PushInheritance<TType, TNode>(GetOrCreateCustomTypeSerializer<TInheritanceHandler>(), parent, child,
            context);
    }

}
