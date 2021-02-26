using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedSet<T> : DeserializationResult<IReadOnlySet<T>>
    {
        public override object? RawValue { get; }

        public override IReadOnlySet<T>? Value { get; }
    }
}
