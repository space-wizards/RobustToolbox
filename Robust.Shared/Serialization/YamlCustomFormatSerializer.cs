using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Helper for doing custom value representation in YAML.
    /// </summary>
    /// <typeparam name="T">The type for which this gives a custom representation.</typeparam>
    public class YamlCustomFormatSerializer<T> : YamlObjectSerializer.TypeSerializer
    {
        private readonly WithFormat<T> _formatter;

        public YamlCustomFormatSerializer(WithFormat<T> formatter)
        {
            _formatter = formatter;
        }

        public override object NodeToType(Type _type, YamlNode node, YamlObjectSerializer serializer)
        {
            return _formatter.FromCustomFormat(serializer.NodeToType(_formatter.Format, node));
        }

        public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
        {
            var t = (T)obj;
            return serializer.TypeToNode(_formatter.ToCustomFormat(t));
        }
    }

    /// <summary>
    /// Convenience class for static access to custom formatters.
    /// </summary>
    public static class WithFormat
    {
        // This is concurrent because it can possibly be updated from multiple threads
        // calling WithFormat at the same time.
        private static readonly ConcurrentDictionary<Type, WithFormat<int>> _flagFormatters = new ConcurrentDictionary<Type, WithFormat<int>>();

        public static WithFormat<int> Flags<T>()
        {
            return _flagFormatters.GetOrAdd(typeof(T), _type => new WithFlagRepresentation<T>());
        }
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
