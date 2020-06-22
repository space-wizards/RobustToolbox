using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Robust.Shared.Utility
{

    [PublicAPI]
    public struct UniqueIndex<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ISet<TValue>>>
    {

        private ImmutableDictionary<TKey, ISet<TValue>> _index;

        public int KeyCount => _index.Count;

        public bool Add(TKey key, TValue value)
        {
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.TryGetValue(key, out var set))
            {
                return set.Add(value);
            }

            _index = _index.Add(key, new HashSet<TValue> {value});
            return true;
        }

        public int AddRange(TKey key, IEnumerable<TValue> values)
        {
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.TryGetValue(key, out var set))
            {
                var c = set.Count;

                set.UnionWith(values);

                return set.Count - c;
            }

            _index = _index.Add(key, set = new HashSet<TValue>(values));

            return set.Count;
        }

        public bool Remove(TKey key)
        {
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return false;
            }

            var newIndex = _index.SetItem(key, new HashSet<TValue>());

            if (_index != newIndex)
            {
                return false;
            }

            _index = newIndex;
            return true;
        }

        public bool Remove(TKey key, TValue value)
        {
            // ReSharper disable once InvertIf
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return false;
            }

            return _index.TryGetValue(key, out var set)
                && set.Remove(value);
        }

        public int RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return 0;
            }

            if (!_index.TryGetValue(key, out var set))
            {
                return 0;
            }

            var c = set.Count;

            set.ExceptWith(set);

            return c - set.Count;
        }

        public bool Replace(TKey key, TValue oldValue, TValue newValue)
        {
            if (_index == null)
            {
                _index = ImmutableDictionary<TKey, ISet<TValue>>.Empty;
                return false;
            }

            if (!_index.TryGetValue(key, out var set))
            {
                return false;
            }

            return set.Remove(oldValue)
                && set.Add(newValue);
        }

        public void Touch(TKey key)
        {
            _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

            if (_index.ContainsKey(key)) return;

            _index = _index.Add(key, new HashSet<TValue>());
        }

        /// <exception cref="InvalidOperationException">Already initialized.</exception>
        public void Initialize(IEnumerable<TKey> keys)
            => Initialize(keys.Select(k => new KeyValuePair<TKey, ISet<TValue>>(k, new HashSet<TValue>())));

        /// <exception cref="InvalidOperationException">Already initialized.</exception>
        public void Initialize(IEnumerable<KeyValuePair<TKey, ISet<TValue>>> index)
        {
            if (_index != null) throw new InvalidOperationException("Already initialized.");

            _index = ImmutableDictionary.CreateRange(index);
        }

        public ISet<TValue> this[TKey key]
        {
            get
            {
                _index ??= ImmutableDictionary<TKey, ISet<TValue>>.Empty;

                if (_index.TryGetValue(key, out var set)) return set;

                _index = _index.Add(key, set = new HashSet<TValue>());

                return set;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<TKey, ISet<TValue>>> GetEnumerator() => _index.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
