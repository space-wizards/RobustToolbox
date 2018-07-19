using System;

namespace SS14.Shared.Serialization
{
    public abstract class ObjectSerializer
    {
        public const string LogCategory = "serialization";

        public delegate void ReadFunctionDelegate<in T>(T value);
        public delegate T WriteFunctionDelegate<out T>();

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
        public abstract void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false);

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
            DataField(ref value, name, defaultValue, alwaysWrite);
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
            Func<TSource, TTarget> ReadConvertFunc,
            Func<TTarget, TSource> WriteConvertFunc = null,
            bool alwaysWrite = false
        );

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
            Func<TSource, TTarget> ReadConvertFunc,
            Func<TTarget, TSource> WriteConvertFunc = null,
            bool alwaysWrite = false
        )
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

        public abstract bool TryReadDataField<T>(string name, out T value);
        public abstract bool TryReadDataFieldCached<T>(string name, out T value);


        /// <summary>
        ///     Sets a cached field for this serialization context.
        ///     This field does not get written in any way,
        ///     but can be recalled during other serialization runs with <see cref="GetCacheData" /> or <see cref="TryGetCacheData" />.
        /// </summary>
        /// <param name="key">The cache key to write to.</param>
        /// <param name="value">The object to write.</param>
        public virtual void SetCacheData(string key, object value)
        {
        }
        public virtual T GetCacheData<T>(string key)
        {
            throw new NotImplementedException();
        }
        public virtual bool TryGetCacheData<T>(string key, out T data)
        {
            data = default(T);
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
        public abstract void DataWriteFunction<T>(string name, T defaultValue, WriteFunctionDelegate<T> func, bool alwaysWrite = false);

        public virtual void DataReadWriteFunction<T>(string name, T defaultValue, ReadFunctionDelegate<T> readFunc, WriteFunctionDelegate<T> writeFunc, bool alwaysWrite = false)
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
    }
}
