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
    public sealed class Vector3Serializer : ITypeSerializer<Vector3, ValueDataNode>, ITypeCopyCreator<Vector3>
    {
        public Vector3 Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Vector3>? instanceProvider = null)
        {
            if (!VectorSerializerUtility.TryParseArgs(node.Value, 3, out var args))
            {
                throw new InvalidMappingException($"Could not parse {nameof(Vector3)}: '{node.Value}'");
            }

            var x = float.Parse(args[0], CultureInfo.InvariantCulture);
            var y = float.Parse(args[1], CultureInfo.InvariantCulture);
            var z = float.Parse(args[2], CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            if (!VectorSerializerUtility.TryParseArgs(node.Value, 3, out var args))
            {
                return new ErrorNode(node, "Failed parsing values for Vector3.");
            }

            return float.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing values for Vector3.");
        }

        public DataNode Write(ISerializationManager serializationManager, Vector3 value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.X.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.Y.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.Z.ToString(CultureInfo.InvariantCulture)}");
        }

        public Vector3 CreateCopy(ISerializationManager serializationManager, Vector3 source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new(source);
        }
    }
}
