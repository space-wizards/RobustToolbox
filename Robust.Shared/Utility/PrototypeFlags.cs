using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Data structure for storing prototype IDs, and ensuring that all stored IDs resolve to valid prototypes.
    /// </summary>
    /// <typeparam name="T">The prototype variant.</typeparam>
    public sealed class PrototypeFlags<T> : IReadOnlyPrototypeFlags<T>
        where T : class, IPrototype
    {
        private readonly HashSet<string> _flags;

        #region Constructors

        public PrototypeFlags()
        {
            _flags = new HashSet<string>();
        }

        public PrototypeFlags(params string[] flags)
        {
            _flags = new HashSet<string>(flags);
        }

        public PrototypeFlags(IEnumerable<string> flags)
        {
            _flags = new HashSet<string>(flags);
        }

        #endregion

        #region API

        public int Count => _flags.Count;

        /// <summary>
        ///     Adds a prototype flag, but only if it's valid.
        /// </summary>
        /// <param name="flag">The prototype identifier.</param>
        /// <param name="prototypeManager">The prototype manager containing the prototype.</param>
        /// <returns>Whether the flag was added or not.</returns>
        public bool Add(string flag, IPrototypeManager prototypeManager)
        {
            return !prototypeManager.TryIndex<T>(flag, out _) && _flags.Add(flag);
        }

        /// <summary>
        ///     Checks whether a specific flag is contained here or not.
        /// </summary>
        public bool Contains(string flag)
        {
            return _flags.Contains(flag);
        }

        /// <summary>
        ///     Checks whether all specified flags are contained here or not.
        /// </summary>
        public bool ContainsAll(IEnumerable<string> flags)
        {
            foreach (var flag in flags)
            {
                if (!Contains(flag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks whether all specified flags are contained here or not.
        /// </summary>
        public bool ContainsAll(params string[] flags)
        {
            return ContainsAll((IEnumerable<string>) flags);
        }

        /// <summary>
        ///     Checks whether any of the specified flags are contained here or not.
        /// </summary>
        public bool ContainsAny(IEnumerable<string> flags)
        {
            foreach (var flag in flags)
            {
                if (Contains(flag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Checks whether any of the specified flags are contained here or not.
        /// </summary>
        public bool ContainsAny(params string[] flags)
        {
            return ContainsAny((IEnumerable<string>) flags);
        }

        /// <summary>
        ///     Removes a flag, if present.
        /// </summary>
        /// <param name="flag">The flag to be removed.</param>
        /// <returns>Whether the prototype flag was successfully removed.</returns>
        public bool Remove(string flag)
        {
            return _flags.Remove(flag);
        }

        /// <summary>
        ///     Empties the collection of all prototype flags.
        /// </summary>
        public void Clear()
        {
            _flags.Clear();
        }

        #endregion

        #region Enumeration

        public IEnumerable<T> GetPrototypes(IPrototypeManager prototypeManager)
        {
            foreach (var prototype in _flags)
            {
                yield return prototypeManager.Index<T>(prototype);
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _flags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public interface IReadOnlyPrototypeFlags<T> : IEnumerable<string>
        where T : class, IPrototype
    {
        int Count { get; }

        bool Contains(string flag);
        bool ContainsAll(IEnumerable<string> flags);
        bool ContainsAll(params string[] flags);
        bool ContainsAny(IEnumerable<string> flags);
        bool ContainsAny(params string[] flags);

        /// <summary>
        ///     Enumerates all prototype flags and returns their actual prototype instances.
        /// </summary>
        /// <param name="prototypeManager">The prototype manager where the prototypes are stored.</param>
        /// <exception cref="InvalidOperationException">Thrown if prototypes in the <see cref="IPrototypeManager"/>
        ///     haven't been loaded yet.
        /// </exception>
        /// <exception cref="UnknownPrototypeException">Thrown if any of the prototype flags in this class does not
        ///     correspond to a valid, known prototype in the <see cref="IPrototypeManager"/>.
        /// </exception>
        /// <returns>The prototype instances for all prototype flags in this object.</returns>
        IEnumerable<T> GetPrototypes(IPrototypeManager prototypeManager);
    }
}
