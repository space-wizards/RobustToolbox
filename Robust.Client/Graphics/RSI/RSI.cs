using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Collections;
using Robust.Shared.ViewVariables;

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
        public Vector2u Size { get; private set; }
        private Dictionary<StateId, State> States = new Dictionary<StateId, State>();

        public State this[StateId key]
        {
            get => States[key];
        }

        public void AddState(State state)
        {
            States[state.StateId] = state;
        }

        public void RemoveState(StateId stateId)
        {
            States.Remove(stateId);
        }

        public bool TryGetState(StateId stateId, out State state)
        {
            return States.TryGetValue(stateId, out state);
        }

        public RSI(Vector2u size)
        {
            Size = size;
        }

        public IEnumerator<State> GetEnumerator()
        {
            return States.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Flags]
        public enum Selectors
        {
            None = 0,
        }

        /// <summary>
        ///     Represents a name+selector pair used to reference states in an RSI.
        /// </summary>
        public struct StateId
        {
            public readonly string Name;
            public readonly Selectors Selectors;

            public StateId(string name, Selectors selectors = Selectors.None)
            {
                Name = name;
                Selectors = selectors;
            }

            /// <summary>
            ///     Effectively the "null" of <c>StateId</c>, because you can't have a null for structs.
            /// </summary>
            public static readonly StateId Invalid = new StateId(null, Selectors.None);
            public bool IsValid => Name != null;

            public override string ToString()
            {
                return Name;
            }

            public static implicit operator StateId(string key)
            {
                return new StateId(key, Selectors.None);
            }

            public override bool Equals(object obj)
            {
                return obj is StateId id && Equals(id);
            }

            public bool Equals(StateId id)
            {
                return id.Name == Name && id.Selectors == Selectors;
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
                return Name.GetHashCode() ^ Selectors.GetHashCode();
            }
        }
    }
}
