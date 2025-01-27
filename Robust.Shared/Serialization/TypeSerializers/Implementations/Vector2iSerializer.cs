using System.Globalization;
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
    public sealed class Vector2iSerializer : ITypeSerializer<Vector2i, ValueDataNode>, ITypeCopyCreator<Vector2i>
    {
        public Vector2i Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Vector2i>? instanceProvider = null)
        {
            if (!VectorSerializerUtility.TryParseArgs(node.Value, 2, out var args))
            {
                throw new InvalidMappingException($"Could not parse {nameof(Vector2i)}: '{node.Value}'");
            }

            var x = int.Parse(args[0], CultureInfo.InvariantCulture);
            var y = int.Parse(args[1], CultureInfo.InvariantCulture);
            return new Vector2i(x, y);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            if (!VectorSerializerUtility.TryParseArgs(node.Value, 2, out var args))
            {
                return new ErrorNode(node, "Failed parsing values for Vector2i.");
            }

            return int.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   int.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing values for Vector2i.");
        }

        public DataNode Write(ISerializationManager serializationManager, Vector2i value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.X.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.Y.ToString(CultureInfo.InvariantCulture)}");
        }

        public Vector2i CreateCopy(ISerializationManager serializationManager, Vector2i source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new(source.X, source.Y);
        }
    }
}
