using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;

namespace Robust.Shared.Serialization
{
    /// <summary>
    ///     Handles serialization of objects to/from a storage medium (which medium is implementation defined).
    ///     Provides methods for most common cases of data field reading/writing.
    /// </summary>
    /// <remarks>
    ///     Object serialization may be "cached".
    ///     This is a non-guaranteed on-request case where, if the serializer is sure it's deserialized something before,
    ///     it can return a previous instance of the value instead of running deserialization logic again.
    ///     Caching only occurs for cases where data would the same, e.g. deserializing the same prototype twice.
    ///     Most methods that read data have a cached variant, which MAY return data shared with other objects (for reference objects).
    ///     This is not guaranteed and should be considered a situational optimization only.
    ///     It is also possible to write cached "data" fields that are not stored anywhere, but can be referenced later in other deserialization runs.
    ///     This is useful for complex cases like sprites which cannot be expressed with a single cached field read, but would like to still take advantage of caching.
    ///     The persistence of this cached data is in no way guaranteed.
    /// </remarks>
    public abstract class ObjectSerializer
    {
        public const string LogCategory = "serialization";

        public delegate void ReadFunctionDelegate<in T>(T value);

        public delegate T WriteFunctionDelegate<out T>();

        public delegate TSource WriteConvertFunc<in TTarget, out TSource>(TTarget target) where TSource : notnull;

        public delegate TTarget ReadConvertFunc<out TTarget, in TSource>(TSource source) where TSource : notnull;

        // Must be set for Expression DataField to work  to ensure no sandbox escape.
        internal Type? CurrentType { get; set; }

        public void SetDeserializingType(Type type)
        {
            if (!IoCManager.Resolve<IModLoader>().IsContentTypeAccessAllowed(type))
            {
                throw new SandboxArgumentException("Type access for this type not allowed.");
            }

            CurrentType = type;
        }

        /// <summary>
        ///     True if this serializer is reading, false if it is writing.
        /// </summary>
        public bool Reading { get; protected set; }

        public bool Writing => !Reading;

        /// <summary>
        ///     Writes or reads a simple field by reference.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        public void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            DataField<T>(ref value, name, defaultValue, WithFormat<T>.NoFormat, alwaysWrite);
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        //DO NOT, I REPEAT DO NOT CHANGE NULLABLE<T> TO T? IT WILL FUCK EVERYTHING UP CAUSE GENERICS AND NULLABILITY DONT MIX
        public void NullableDataField<T>(ref Nullable<T> value, string name, Nullable<T> defaultValue) where T : unmanaged
        {
            NullableDataField(ref value, name, defaultValue, WithFormat<T>.NoFormat);
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="withFormat">The formatter to use for representing this particular value in the medium.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        //DO NOT, I REPEAT DO NOT CHANGE NULLABLE<T> TO T? IT WILL FUCK EVERYTHING UP CAUSE GENERICS AND NULLABILITY DONT MIX
        public void NullableDataField<T>(ref Nullable<T> value, string name, Nullable<T> defaultValue, WithFormat<T> withFormat)
            where T : unmanaged
        {
            if (Reading)
            {
                var tempValue = default(T);
                if (!TryReadDataField<T>(name, withFormat, out tempValue))
                {
                    value = null;
                }
                else
                {
                    value = tempValue;
                }
            }
            else
            {
                if(EqualityComparer<T?>.Default.Equals(value, defaultValue)) return;

                if (value == null)
                {
                    DataField(ref value, name, defaultValue);
                }
                else
                {
                    var tempValue = (T) value;
                    DataField(ref tempValue, name, (T)defaultValue!, withFormat);
                }
            }
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        public void NullableDataField<T>(ref T? value, string name, T? defaultValue)
        {
            NullableDataField(ref value, name, defaultValue, WithFormat<T>.NoFormat);
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="withFormat">The formatter to use for representing this particular value in the medium.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        public void NullableDataField<T>(ref T? value, string name, T? defaultValue, WithFormat<T> withFormat)
        {
            if (Reading)
            {
                if (!TryReadDataField(name, withFormat, out value))
                {
                    value = default;
                }
            }
            else
            {
                if(EqualityComparer<T?>.Default.Equals(value, defaultValue)) return;

                if (value == null)
                {
                    DataField(ref value, name, defaultValue);
                }
                else
                {
                    DataField<T>(ref value, name, (T)defaultValue, withFormat);
                }
            }
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="withFormat">The formatter to use for representing this particular value in the medium.</param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        public abstract void DataField<T>(ref T value, string name, T defaultValue, WithFormat<T> withFormat,
            bool alwaysWrite = false);

        /// <summary>
        ///     Writes or reads a field or property via reflection.
        /// </summary>
        /// <param name="root">The reference to the object that has the property or field referenced.</param>
        /// <param name="expr">The reference to the field or property that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="TRoot">The type of the object that has the property or field referenced.</typeparam>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        /// <remarks>
        /// <example>
        ///     szr.DataField(this, x => x.SomeProperty, "some-property", SomeDefaultValue);
        /// </example>
        /// </remarks>
        public virtual void DataField<TRoot, T>(TRoot o, Expression<Func<TRoot, T>> expr, string name, T defaultValue,
            bool alwaysWrite = false)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            if (expr == null)
            {
                throw new ArgumentNullException(nameof(expr));
            }

            if (!(expr.Body is MemberExpression mExpr))
            {
                throw new NotSupportedException("Cannot handle expressions of types other than MemberExpression.");
            }

            if ((CurrentType == null || !o.GetType().IsAssignableTo(CurrentType)) &&
                !IoCManager.Resolve<IModLoader>().IsContentTypeAccessAllowed(o.GetType()))
            {
                throw new SandboxArgumentException(
                    "Expression-based DataField requires this serializer to be correctly configured for sandboxing.");
            }

            WriteFunctionDelegate<T> getter;
            ReadFunctionDelegate<T> setter;
            switch (mExpr.Member)
            {
                case FieldInfo fi:
                    getter = () => (T) fi.GetValue(o)!;
                    setter = v => fi.SetValue(o, v);
                    break;
                case PropertyInfo pi:
                    getter = () => (T) pi.GetValue(o)!;
                    setter = v => pi.SetValue(o, v);
                    break;
                default:
                    throw new NotSupportedException(
                        "Cannot handle member expressions of types other than FieldInfo or PropertyInfo.");
            }

            DataReadWriteFunction(name, defaultValue, setter, getter, alwaysWrite);
        }


        /// <summary>
        ///     Writes or reads a simple field by reference.
        ///     This method can cache results and share them with other objects.
        ///     As such, when reading, your value may NOT be private. Do not modify it as if it's purely your own.
        ///     This can cut out parsing steps and memory cost for commonly used objects such as walls.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        public virtual void DataFieldCached<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false)
        {
            DataField(ref value, name, defaultValue, WithFormat<T>.NoFormat, alwaysWrite);
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        ///     This method can cache results and share them with other objects.
        ///     As such, when reading, your value may NOT be private. Do not modify it as if it's purely your own.
        ///     This can cut out parsing steps and memory cost for commonly used objects such as walls.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="withFormat">The formatter to use for representing this particular value in the medium.</param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="T">The type of the field that will be read/written.</typeparam>
        public virtual void DataFieldCached<T>(ref T value, string name, T defaultValue, WithFormat<T> withFormat,
            bool alwaysWrite = false)
        {
            DataField(ref value, name, defaultValue, withFormat, alwaysWrite);
        }

        /// <summary>
        ///     Writes or reads a simple field by reference.
        ///     Runs the provided delegate to do conversion from a more easy to (de)serialize data type.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="ReadConvertFunc">
        ///     A delegate invoked to convert the intermediate serialization object <typeparamref name="TSource" />
        ///     to the actual value <typeparamref name="TTarget" /> while reading.
        /// </param>
        /// <param name="WriteConvertFunc">
        ///     A delegate invoked to convert the actual value <typeparamref name="TTarget" />
        ///     to an intermediate serialization object <typeparamref name="TSource" /> that will be written.
        /// </param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="TTarget">The type of the field that will be read/written.</typeparam>
        /// <typeparam name="TSource">The type of the intermediate object that will be (de)serialized.</typeparam>
        public abstract void DataField<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            ReadConvertFunc<TTarget, TSource> ReadConvertFunc,
            WriteConvertFunc<TTarget, TSource>? WriteConvertFunc = null,
            bool alwaysWrite = false
        ) where TSource : notnull;

        /// <summary>
        ///     Writes or reads a simple field by reference.
        ///     This method can cache results and share them with other objects.
        ///     As such, when reading, your value may NOT be private.
        ///     This can cut out parsing steps and memory cost for commonly used objects such as walls.
        ///     This method can cache results and share them with other objects.
        ///     As such, when reading, your value may NOT be private. Do not modify it as if it's purely your own.
        ///     This can cut out parsing steps and memory cost for commonly used objects such as walls.
        /// </summary>
        /// <param name="value">The reference to the field that will be read/written into.</param>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value. Used if the field does not exist while reading or to know if writing would be redundant.</param>
        /// <param name="ReadConvertFunc">
        ///     A delegate invoked to convert the intermediate serialization object <typeparamref name="TSource" />
        ///     to the actual value <typeparamref name="TTarget" /> while reading.
        /// </param>
        /// <param name="WriteConvertFunc">
        ///     A delegate invoked to convert the actual value <typeparamref name="TTarget" />
        ///     to an intermediate serialization object <typeparamref name="TSource" /> that will be written.
        /// </param>
        /// <param name="alwaysWrite">If true, always write this field to map saving, even if it matches the default.</param>
        /// <typeparam name="TTarget">The type of the field that will be read/written.</typeparam>
        /// <typeparam name="TSource">The type of the intermediate object that will be (de)serialized.</typeparam>
        public virtual void DataFieldCached<TTarget, TSource>(
            ref TTarget value,
            string name,
            TTarget defaultValue,
            ReadConvertFunc<TTarget, TSource> ReadConvertFunc,
            WriteConvertFunc<TTarget, TSource>? WriteConvertFunc = null,
            bool alwaysWrite = false
        ) where TSource : notnull
        {
            DataField(ref value, name, defaultValue, ReadConvertFunc, WriteConvertFunc, alwaysWrite);
        }

        /// <summary>
        ///     While reading, reads a data field and immediately returns it as value.
        /// </summary>
        /// <param name="name">The name of the field to read.</param>
        /// <param name="defaultValue">Default value of the field if it does not exist.</param>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <returns>The value of the field.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the reader is not currently reading.
        /// </exception>
        public abstract T ReadDataField<T>(string name, T defaultValue);

        /// <summary>
        ///     While reading, reads a data field and immediately returns it as value.
        /// </summary>
        /// <param name="name">The name of the field to read.</param>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <returns>The value of the field.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the reader is not currently reading.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown if the field does not exist.
        /// </exception>
        public virtual T ReadDataField<T>(string name)
        {
            if (TryReadDataField<T>(name, out var val))
            {
                return val;
            }

            throw new KeyNotFoundException(name);
        }

        /// <summary>
        ///     While reading, reads a data field and immediately returns it as value.
        ///     This method can cache results and share them with other objects.
        ///     As such, when reading, your value may NOT be private. Do not modify it as if it's purely your own.
        ///     This can cut out parsing steps and memory cost for commonly used objects such as walls.
        /// </summary>
        /// <param name="name">The name of the field to read.</param>
        /// <param name="defaultValue">Default value of the field if it does not exist.</param>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <returns>The value of the field.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the reader is not currently reading.
        /// </exception>
        public virtual T ReadDataFieldCached<T>(string name, T defaultValue)
        {
            return ReadDataField(name, defaultValue);
        }

        /// <summary>
        ///     Try- pattern version of <see cref="ReadDataField" />.
        /// </summary>
        public virtual bool TryReadDataField<T>(string name, [MaybeNullWhen(false)] out T value)
        {
            return TryReadDataField(name, WithFormat<T>.NoFormat, out value);
        }

        public abstract bool TryReadDataField<T>(string name, WithFormat<T> format, [MaybeNullWhen(false)] out T value);

        /// <summary>
        ///     Try- pattern version of <see cref="ReadDataFieldCached" />.
        /// </summary>
        public virtual bool TryReadDataFieldCached<T>(string name, [MaybeNullWhen(false)] out T value)
        {
            return TryReadDataFieldCached(name, WithFormat<T>.NoFormat, out value);
        }

        public virtual bool TryReadDataFieldCached<T>(string name, WithFormat<T> format,
            [MaybeNullWhen(false)] out T value)
        {
            return TryReadDataField(name, format, out value);
        }

        public abstract void WriteDataField<T>(string name, T value, T defaultValue, bool alwaysWrite = false);

        /// <summary>
        ///     Sets a cached field for this serialization context.
        ///     This field does not get written in any way,
        ///     but can be recalled during other serialization runs of the same data with <see cref="GetCacheData" /> or <see cref="TryGetCacheData" />.
        /// </summary>
        /// <param name="key">The cache key to write to.</param>
        /// <param name="value">The object to write.</param>
        public virtual void SetCacheData(string key, object value)
        {
        }

        /// <summary>
        ///     Gets cached data set by <see cref="SetCacheData" /> in a previous deserialization run of the same data.
        /// </summary>
        /// <param name="key">The key to recall.</param>
        /// <typeparam name="T">The type to cast the return object to.</typeparam>
        /// <returns>The data previously stored.</returns>
        public virtual T GetCacheData<T>(string key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Try- pattern version of <see cref="GetCacheData" />.
        /// </summary>
        public virtual bool TryGetCacheData<T>(string key, [MaybeNullWhen(false)] out T data)
        {
            data = default;
            return false;
        }

        /// <summary>
        ///     Provides a delegate to parse data from a more simpler type that can be mapped to mediums like YAML.
        ///     This is useful if your data does not map 1:1 to the prototype.
        /// </summary>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">A default value to read in case the field is not exist.</param>
        /// <param name="func">A delegate that takes in the simpler data and is expected to set internal state on the caller.</param>
        /// <typeparam name="T">The type of the data that will be read from the storage medium.</typeparam>
        public abstract void DataReadFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> func);

        /// <summary>
        ///     Provides a delegate to write custom data to a more simpler type that can be mapped to mediums like YAML.
        ///     This is useful if your data does not map 1:1 to the prototype.
        /// </summary>
        /// <param name="name">The name of the field in the serialization medium. Most likely the name in YAML.</param>
        /// <param name="defaultValue">The default value. Used to check if writing can be skipped when <paramref name="alwaysWrite" /> is true.</param>
        /// <param name="func">A delegate that produces simpler data based on the internal state of the caller.</param>
        /// <param name="alwaysWrite">If true, data will always be written even if it matches <paramref name="defaultValue" />.</param>
        /// <typeparam name="T">The type of the data that will be written to the storage medium.</typeparam>
        public abstract void DataWriteFunction<T>(string name, T defaultValue, WriteFunctionDelegate<T> func,
            bool alwaysWrite = false);

        /// <summary>
        ///     It's <see cref="DataReadFunction" /> and <see cref="DataWriteFunction" /> in one, so you don't need to pass name and default twice!
        ///     Marvelous!
        /// </summary>
        public virtual void DataReadWriteFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> readFunc,
            WriteFunctionDelegate<T> writeFunc, bool alwaysWrite = false)
        {
            if (Reading)
            {
                DataReadFunction(name, defaultValue, readFunc);
            }
            else
            {
                DataWriteFunction(name, defaultValue, writeFunc, alwaysWrite);
            }
        }

        /// <summary>
        ///     Returns a "string or enum" key value from a field.
        ///     These values are either a string, or an enum.
        ///     This is good for identifiers that can either be a string (any value, prototypes go wild),
        ///     or an enum when type safety is required for the code.
        /// </summary>
        /// <param name="fieldName">The name of the field to read the key from.</param>
        /// <seealso cref="IReflectionManager.TryParseEnumReference"/>
        public virtual object ReadStringEnumKey(string fieldName)
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var keyString = ReadDataField<string>(fieldName);
            if (reflectionManager.TryParseEnumReference(keyString, out var @enum))
            {
                return @enum;
            }

            return keyString;
        }

        public abstract ObjectSerializer ReadDataFieldFork<T>(string name);

        public abstract ObjectSerializer WriteDataFieldFork<T>(string name, T value) where T : notnull;
    }
}
