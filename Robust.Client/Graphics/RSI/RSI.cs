using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using SixLabors.ImageSharp.Processing.Processors.Dithering;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Type to handle Robust Station Image (RSI) files.
    /// </summary>
    public sealed partial class RSI : IEnumerable<RSI.State>
    {
        /// <summary>
        ///     The size of this RSI, width x height.
        /// </summary>
        [ViewVariables]
        public Vector2i Size { get; private set; }
        private Dictionary<StateId, State> States = new Dictionary<StateId, State>();

        public State this[StateId key] => States[key];

        public void AddState(State state)
        {
            States[state.StateId] = state;
        }

        public void RemoveState(StateId stateId)
        {
            States.Remove(stateId);
        }

        public bool TryGetState(StateId stateId, [NotNullWhen(true)] out State? state)
        {
            return States.TryGetValue(stateId, out state);
        }

        public RSI(Vector2i size)
        {
            Size = size;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        public RSI(RSI toCopy)
        {
            Size = toCopy.Size;
            // should be fine to just reuse the values of the other dictionary because they seem to be immutable
            States = toCopy.States.ToDictionary(entry => entry.Key,
                entry => entry.Value);
        }

        public IEnumerator<State> GetEnumerator()
        {
            return States.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Represents an ID used to reference states in an RSI.
        ///     Kept around as a simple wrapper around the state Name, to avoid breaking existing code.
        /// </summary>
        public struct StateId : IEquatable<StateId>
        {
            public readonly string? Name;

            public StateId(string? name)
            {
                Name = name;
            }

            /// <summary>
            ///     Effectively the "null" of <c>StateId</c>, because you can't have a null for structs.
            /// </summary>
            public static readonly StateId Invalid = default;
            public bool IsValid => Name != null;

            public override string? ToString()
            {
                return Name;
            }

            public static implicit operator StateId(string? key)
            {
                return new StateId(key);
            }

            public override bool Equals(object? obj)
            {
                return obj is StateId id && Equals(id);
            }

            public bool Equals(StateId id)
            {
                return id.Name == Name;
            }

            public static bool operator ==(StateId a, StateId b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(StateId a, StateId b)
            {
                return !a.Equals(b);
            }

            public override int GetHashCode()
            {
                return Name?.GetHashCode() ?? 0;
            }
        }
    }
}
