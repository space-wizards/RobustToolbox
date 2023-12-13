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
    public sealed class Box2Serializer :
        ITypeSerializer<Box2, ValueDataNode>,
        ITypeCopyCreator<Box2>,
        ITypeSerializer<Box2i, ValueDataNode>,
        ITypeCopyCreator<Box2i>
    {
        #region Box2

        public Box2 Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Box2>? instanceProvider = null)
        {
            var nodeValue = node.Value.AsSpan();
            NextOrThrow(ref nodeValue, out var current, node.Value);

            var l = Parse.Float(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var b = Parse.Float(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var r = Parse.Float(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var t = Parse.Float(current);

            return new Box2(l, b, r, t);
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

        ValidationNode ITypeValidator<Box2, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
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

        [MustUseReturnValue]
        public Box2 CreateCopy(ISerializationManager serializationManager, Box2 source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new Box2(source.Left, source.Bottom, source.Right, source.Top);
        }

        #endregion

        #region Box2i

        public Box2i Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Box2i>? instanceProvider = null)
        {
            var nodeValue = node.Value.AsSpan();
            NextOrThrow(ref nodeValue, out var current, node.Value);

            var l = Parse.Int32(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var b = Parse.Int32(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var r = Parse.Int32(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var t = Parse.Int32(current);

            return new Box2i(l, b, r, t);
        }

        public DataNode Write(ISerializationManager serializationManager, Box2i value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            var nodeValue =
                $"{value.Left.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Bottom.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Right.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Top.ToString(CultureInfo.InvariantCulture)}";

            return new ValueDataNode(nodeValue);
        }

        ValidationNode ITypeValidator<Box2i, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            var args = node.Value.Split(',');

            if (args.Length != 4)
            {
                return new ErrorNode(node, "Invalid amount of args for Box2i.");
            }

            return Parse.TryInt32(args[0], out _) &&
                   Parse.TryInt32(args[1], out _) &&
                   Parse.TryInt32(args[2], out _) &&
                   Parse.TryInt32(args[3], out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing values of Box2i.");
        }

        public Box2i CreateCopy(ISerializationManager serializationManager, Box2i source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new Box2i(source.Left, source.Bottom, source.Right, source.Top);
        }

        #endregion

        private static void NextOrThrow(
            ref ReadOnlySpan<char> source,
            out ReadOnlySpan<char> splitValue,
            string errValue)
        {
            if (!SpanSplitExtensions.SplitFindNext(ref source, ',', out splitValue))
                throw new InvalidMappingException($"Could not parse {nameof(Box2)}: '{errValue}'");
        }
    }
}
