using System.Globalization;
using System.Numerics;
using Robust.Shared.IoC;
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
    public sealed class Matrix3x2Serializer : ITypeSerializer<Matrix3x2, ValueDataNode>, ITypeCopyCreator<Matrix3x2>
    {
        public Matrix3x2 Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Matrix3x2>? instanceProvider = null)
        {
            if (!VectorSerializerUtility.TryParseArgs(node.Value, 6, out var args))
            {
                throw new InvalidMappingException($"Could not parse {nameof(Matrix3x2)}: '{node.Value}'");
            }

            var data = new float[6];
            for (int i = 0; i < 6; i++)
            {
                data[i] = float.Parse(args[i], CultureInfo.InvariantCulture);
            }
            return new Matrix3x2(data[0], data[1], data[2], data[3], data[4], data[5]);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            if (!VectorSerializerUtility.TryParseArgs(node.Value, 6, out var args))
            {
                throw new InvalidMappingException($"Could not parse {nameof(Matrix3x2)}: '{node.Value}'");
            }

            return float.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[4], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[5], NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing values for Matrix3x2.");
        }

        public DataNode Write(ISerializationManager serializationManager, Matrix3x2 value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.M11.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.M12.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.M21.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.M22.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.M31.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{value.M32.ToString(CultureInfo.InvariantCulture)}");
        }

        public Matrix3x2 CreateCopy(ISerializationManager serializationManager, Matrix3x2 source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new(source.M11, source.M12, source.M21, source.M22, source.M31, source.M32);
        }
    }
}
