namespace SS14.Shared.Serialization
{
    public abstract class ObjectSerializer
    {
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
    }
}
