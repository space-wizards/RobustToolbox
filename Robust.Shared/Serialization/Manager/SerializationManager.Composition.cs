using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
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
            var dependencyCollectionConst = Expression.Constant(instance.DependencyCollection);

            var typeParam = Expression.Parameter(typeof(Type), "type");
            var parentParam = Expression.Parameter(nodeType, "parent");
            var childParam = Expression.Parameter(nodeType, "child");
            //todo paul serializers in the context should also override default serializers for array etc
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            Expression expression;

            if (instance.TryGetTypeInheritanceHandler(value, nodeType, out var handler))
            {
                var readerType = typeof(ITypeInheritanceHandler<,>).MakeGenericType(value, nodeType);
                var readerConst = Expression.Constant(handler, readerType);

                expression = Expression.Call(
                    readerConst,
                    "PushInheritance",
                    Type.EmptyTypes,
                    instanceConst,
                    childParam,
                    parentParam,
                    dependencyCollectionConst,
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
                        childParam,
                        parentParam),
                    MappingDataNode => Expression.Call(
                        instanceConst,
                        nameof(PushInheritanceMapping),
                        Type.EmptyTypes,
                        childParam,
                        parentParam),
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
        return child; //todo implement different inheritancebehaviours
    }

    private MappingDataNode PushInheritanceMapping(MappingDataNode child, MappingDataNode parent)
    {
        return child; //todo implement different inheritancebehaviours
    }

}
