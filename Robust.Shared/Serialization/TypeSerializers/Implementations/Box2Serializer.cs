using System;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class Box2Serializer : ITypeSerializer<Box2, ValueDataNode>
    {
        private static void NextOrThrow(ref SpanSplitExtensions.Enumerator<char> enumerator, string value)
        {
            if (!enumerator.MoveNext())
                throw new InvalidMappingException($"Could not parse {nameof(Box2)}: '{value}'");
        }

        public Box2 Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, Box2 value = default)
        {
            var nodeValue = node.Value;
            var args = nodeValue.AsSpan().Split(',').GetEnumerator();
            NextOrThrow(ref args, nodeValue);

            var l = Parse.Float(args.Current);
            NextOrThrow(ref args, nodeValue);

            var b = Parse.Float(args.Current);
            NextOrThrow(ref args, nodeValue);

            var r = Parse.Float(args.Current);
            NextOrThrow(ref args, nodeValue);

            var t = Parse.Float(args.Current);

            return new Box2(l, b, r, t);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var args = node.Value.Split(',');

            if (args.Length != 4)
            {
                return new ErrorNode(node, "Invalid amount of args for Box2.");
            }

            return Parse.TryFloat(args[0], out _) &&
                   Parse.TryFloat(args[1], out _) &&
                   Parse.TryFloat(args[2], out _) &&
                   Parse.TryFloat(args[3], out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing values of Box2.");
        }

        public DataNode Write(ISerializationManager serializationManager, Box2 value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var nodeValue =
                $"{value.Left.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Bottom.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Right.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Top.ToString(CultureInfo.InvariantCulture)}";

            return new ValueDataNode(nodeValue);
        }

        [MustUseReturnValue]
        public Box2 Copy(ISerializationManager serializationManager, Box2 source, Box2 target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.Left, source.Bottom, source.Right, source.Top);
        }
    }
}
