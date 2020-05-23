using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Interfaces.Serialization
{
    /// <summary>
    /// Provides information about custom serialization formats used by certain fields.
    /// </summary>
    public interface ICustomFormatManager
    {
        /// <summary>
        /// Get a custom <c>int</c> format in terms of enum flags, chosen by a tag type.
        /// </summary>
        /// <typeparam name="T">
        /// The tag type to select the representation with. To understand more about how
        /// tag types are used, see the <see cref="FlagsForAttribute"/>.
        /// </typeparam>
        /// <returns>
        /// A custom serialization format for int values, chosen by the tag type.
        /// </returns>
        public WithFormat<int> FlagFormat<T>();
    }


    /// <summary>
    /// Interface for controlling custom value representation in an abstract storage medium.
    /// </summary>
    public abstract class WithFormat<T>
    {
        /// <summary>
        /// The underlying type used for representation in the storage medium.
        /// </summary>
        public abstract Type Format { get; }

        public abstract T FromCustomFormat(object obj);
        public abstract object ToCustomFormat(T t);

        /// <summary>
        /// Get the corresponding YAML serializer for the custom representation.
        /// </summary>
        public virtual YamlObjectSerializer.TypeSerializer GetYamlSerializer()
        {
            return new YamlCustomFormatSerializer<T>(this);
        }

        private class DoNothing<T> : WithFormat<T>
        {
            public override Type Format => typeof(T);
            public override T FromCustomFormat(object obj) { return (T)obj; }
            public override object ToCustomFormat(T t) { return t; }

            private static YamlCustomFormatSerializer<T> _serializer;

            internal DoNothing()
            {
                if (_serializer == null)
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
        public static readonly WithFormat<T> NoFormat = new DoNothing<T>();
    }
}
