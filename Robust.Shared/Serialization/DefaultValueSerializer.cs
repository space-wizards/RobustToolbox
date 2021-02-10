using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Serialization
{
    public sealed class DefaultValueSerializer : ObjectSerializer
    {
        public static DefaultValueSerializer Reader()
        {
            return new()
            {
                Reading = true,
            };
        }

        private DefaultValueSerializer() {}

        public override void DataField<T>(ref T value, string name, T defaultValue, WithFormat<T> withFormat, bool alwaysWrite = false)
        {
            if (Reading)
            {
                if (EqualityComparer<T>.Default.Equals(value, default))
                  value = defaultValue;
            }
        }

        public override T ReadDataField<T>(string name, T defaultValue)
        {
            return defaultValue;
        }

        public override bool TryReadDataField<T>(string name, WithFormat<T> format, [MaybeNullWhen(false)] out T value)
        {
            value = default;
            return false;
        }

        public override void DataField<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            ReadConvertFunc<TTarget, TSource> ReadConvertFunc,
            WriteConvertFunc<TTarget, TSource>? WriteConvertFunc = null,
            bool alwaysWrite = false
        )
        {
            if (Reading)
            {
                if (EqualityComparer<TTarget>.Default.Equals(value, default))
                  value = defaultValue;
            }
        }

        public override void DataReadFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> func)
        {
            if (Reading)
            {
                func(defaultValue);
            }
        }

        public override void DataWriteFunction<T>(string name, T defaultValue, WriteFunctionDelegate<T> func, bool alwaysWrite = false)
        {
        }
    }
}
