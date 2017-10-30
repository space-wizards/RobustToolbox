using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Resources
{
    /// <summary>
    ///     Type to load Robust Station Image (RSI) files.
    /// </summary>
    public class RSI
    {
        public const uint MINIMUM_RSI_VERSION = 1;
        public const uint MAXIMUM_RSI_VERSION = 1;

        public Vector2u Size { get; private set; }
        private Dictionary<StateId, State> States = new Dictionary<StateId, State>();

        public State this[StateId key]
        {
            get => States[key];
            set => States[key] = value;
        }

        public State AddState(StateId stateId)
        {

        }

        [Flags]
        public enum Selectors
        {
            None = 0,
        }

        public class State
        {
            public Vector2u Size { get; private set; }

        }

        /// <summary>
        ///     Represents a name+selector pair used to reference states in an RSI.
        /// </summary>
        public struct StateId
        {
            public readonly string Name;
            public readonly Selectors Selectors;

            public StateId(string name, Selectors selectors)
            {
                Name = name;
                Selectors = selectors;
            }

            public override string ToString()
            {
                return Name;
            }

            public static implicit operator StateId(string key)
            {
                return new StateId(key, Selectors.None);
            }
        }
    }


}
    