using System;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Interface for controlling custom value representation in an abstract storage medium.
    /// </summary>
    public abstract class WithFormat<T>
    {
        /// <summary>
        /// The underlying type used for representation in the storage medium.
        /// </summary>
        public abstract Type Format { get; }

        [return: NotNull]
        public abstract T FromCustomFormat(object obj);
        public abstract object ToCustomFormat(T t); // t is never null here. Promise.

        /// <summary>
        /// Get the corresponding YAML serializer for the custom representation.
        /// </summary>
        public virtual YamlObjectSerializer.TypeSerializer GetYamlSerializer()
        {
            return new YamlCustomFormatSerializer<T>(this);
        }

        private class DoNothing : WithFormat<T>
        {
            public override Type Format => typeof(T);
            [return: NotNull]
            public override T FromCustomFormat(object obj) { return (T)obj; }
            public override object ToCustomFormat(T t) { return t!; }

            private readonly YamlCustomFormatSerializer<T> _serializer;

            internal DoNothing()
            {
                _serializer = new YamlCustomFormatSerializer<T>(this);
            }

            public override YamlObjectSerializer.TypeSerializer GetYamlSerializer()
            {
                return _serializer;
            }
        }

        /// <summary>
        /// The identity format i.e. the format which represents a value as itself.
        /// </summary>
        public static readonly WithFormat<T> NoFormat = new DoNothing();
    }
}
