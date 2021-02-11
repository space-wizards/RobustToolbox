#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Robust.Shared.Utility
{

    /// <summary>
    /// A dictionary of mutable and immutable sets for use as an index of unique values related to another collection.
    /// Imitates the behavior of an index in a RDBMS.
    /// </summary>
    /// <remarks>
    /// Implementations may or may not be intended to be explicitly constructed before use.
    /// Do not use this interface directly as it is inherited by structs which will cause boxing.
    /// This interface is used for conformity and documentation.
    /// </remarks>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    /// <seealso cref="UniqueIndexExtensions"/>
    internal interface IUniqueIndex<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ISet<TValue>>> where TKey : notnull
    {

        /// <summary>
        /// The count of keys (and thus sets) in this index.
        /// </summary>
        [CollectionAccess(CollectionAccessType.Read)]
        int KeyCount { get; }

        /// <summary>
        /// Adds a value.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="value">A value to be added.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        bool Add(TKey key, TValue value);

        /// <summary>
        /// Adds a collection of values to a set.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="values">A collection of values.</param>
        /// <returns>The count of values that were added to the set.</returns>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        int AddRange(TKey key, IEnumerable<TValue> values);

        /// <summary>
        /// Removes a set.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        bool Remove(TKey key);

        /// <summary>
        /// Removes a value.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="value">A value to be removed.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        bool Remove(TKey key, TValue value);

        /// <summary>
        /// Removes a collection of values from a set.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="values">A collection of values.</param>
        /// <returns>The count of values that were removed from the set.</returns>
        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        int RemoveRange(TKey key, IEnumerable<TValue> values);

        /// <summary>
        /// Replaces a old value with a new value.
        /// </summary>
        /// <param name="key">The associated key.</param>
        /// <param name="oldValue">A value to be replaced.</param>
        /// <param name="newValue">A value to replace with.</param>
        /// <returns><c>true</c> upon success, otherwise <c>false</c>.</returns>
        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        bool Replace(TKey key, TValue oldValue, TValue newValue);

        /// <summary>
        /// Ensures an empty mutable set for a given key.
        /// </summary>
        /// <param name="key">A given key.</param>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        void Touch(TKey key);

        /// <summary>
        /// Makes a given key's set immutable.
        /// </summary>
        /// <param name="key">A given key.</param>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        bool Freeze(TKey key);

        /// <summary>
        /// Initializes the index from a collection of keys.
        /// </summary>
        /// <param name="keys">A collection of keys.</param>
        /// <exception cref="InvalidOperationException">Already initialized.</exception>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        void Initialize(IEnumerable<TKey> keys);

        /// <summary>
        /// Initializes the index from an equivalent collection.
        /// </summary>
        /// <param name="index">An equivalent collection.</param>
        /// <exception cref="InvalidOperationException">Already initialized.</exception>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        void Initialize(IEnumerable<KeyValuePair<TKey, ISet<TValue>>> index);

    }

}
