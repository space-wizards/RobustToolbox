namespace Robust.Shared.Serialization
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

        /// <summary>
        /// Get a custom <c>int</c> format in terms of enum constants, chosen by a tag type.
        /// </summary>
        /// <typeparam name="T">
        /// The tag type to select the representation with. To understand more about how
        /// tag types are used, see the <see cref="ConstantsForAttribute"/>.
        /// </typeparam>
        /// <returns>
        /// A custom serialization format for int values, chosen by the tag type.
        /// </returns>
        public WithFormat<int> ConstantFormat<T>();
    }
}