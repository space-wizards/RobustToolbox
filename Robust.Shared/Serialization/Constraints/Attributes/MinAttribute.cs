using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Constraints.Interfaces;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.Constraints.Attributes;

public sealed class MinAttribute : ConstraintAttribute
{
    public double MinValue;

    public MinAttribute(double minValue)
    {
        MinValue = minValue;
    }

    public override Type[] SupportedTypes => new[]
        { typeof(int), typeof(double), typeof(float), typeof(short), typeof(long), typeof(byte) };

    public override bool Validate(ISerializationManager serializationManager, object? value,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return value switch
        {
            double d => d >= MinValue,
            float f => f >= MinValue,
            long l => l >= MinValue,
            int i => i >= MinValue,
            short s => s >= MinValue,
            byte b => b >= MinValue,
            _ => throw new InvalidOperationException($"{nameof(MinAttribute)} provided with unsupported value {value}")
        };
    }
}
