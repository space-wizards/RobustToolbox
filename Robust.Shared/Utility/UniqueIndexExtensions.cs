#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Utility
{

    /// <summary>
    /// Extension methods for <see cref="UniqueIndex{TKey,TValue}"/>.
    /// </summary>
    public static class UniqueIndexExtensions {

        /// <summary>
        /// Completely resets a <see cref="UniqueIndex{TKey,TValue}"/>.
        /// </summary>
        /// <param name="index">A given index.</param>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <seealso cref="UniqueIndex{TKey,TValue}"/>
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public static void Clear<TKey, TValue>(ref this UniqueIndex<TKey, TValue> index) where TKey: notnull => index = default;

    }

}
