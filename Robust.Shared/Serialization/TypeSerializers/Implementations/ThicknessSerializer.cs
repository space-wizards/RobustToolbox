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
    public sealed class ThicknessSerializer : ITypeSerializer<Thickness, ValueDataNode>, ITypeCopyCreator<Thickness>
    {
        public Thickness Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Thickness>? instanceProvider = null)
        {
            var nodeValue = node.Value.AsSpan();
            NextOrThrow(ref nodeValue, out var current, node.Value);

            var left = Parse.Float(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var top = Parse.Float(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var right = Parse.Float(current);
            NextOrThrow(ref nodeValue, out current, node.Value);

            var bottom = Parse.Float(current);

            return new Thickness(left, top, right, bottom);
        }

        public DataNode Write(ISerializationManager serializationManager, Thickness value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var nodeValue =
                $"{value.Left.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Top.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Right.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.Bottom.ToString(CultureInfo.InvariantCulture)}";

            return new ValueDataNode(nodeValue);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            var args = node.Value.Split(',');

            if (args.Length != 4)
            {
                return new ErrorNode(node, "Invalid amount of args for Thickness.");
            }

            if (!Parse.TryFloat(args[0], out _) ||
                !Parse.TryFloat(args[1], out _) ||
                !Parse.TryFloat(args[2], out _) ||
                !Parse.TryFloat(args[3], out _))
            {
                return new ErrorNode(node, "Failed parsing values of Thickness.");
            }

            return new ValidatedValueNode(node);
        }

        [MustUseReturnValue]
        public Thickness CreateCopy(ISerializationManager serializationManager, Thickness source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new Thickness(source.Left, source.Top, source.Right, source.Bottom);
        }

        private static void NextOrThrow(
            ref ReadOnlySpan<char> source,
            out ReadOnlySpan<char> splitValue,
            string errValue)
        {
            if (!SpanSplitExtensions.SplitFindNext(ref source, ',', out splitValue))
                throw new InvalidMappingException($"Could not parse {nameof(Thickness)}: '{errValue}'");
        }
    }
}
