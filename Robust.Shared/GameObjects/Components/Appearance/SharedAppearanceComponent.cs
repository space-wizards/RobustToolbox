using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     The appearance component allows game logic to be more detached from the actual visuals of an entity such as 2D sprites, 3D, particles, lights...
    ///     It does this with a "data" system. Basically, code writes data to the component, and the component will use prototype-based configuration to change the actual visuals.
    ///     The data works using a simple key/value system. It is recommended to use enum keys to prevent errors.
    ///     Visualization works client side with overrides of the <c>AppearanceVisualizer</c> class.
    /// </summary>
    [NetID(NetIDs.APPEARANCE)]
    public abstract class SharedAppearanceComponent : Component
    {
        public override string Name => "Appearance";

        public abstract void SetData(string key, object value);
        public abstract void SetData(Enum key, object value);

        public abstract T GetData<T>(string key);
        public abstract T GetData<T>(Enum key);

        public abstract bool TryGetData<T>(string key, [NotNullWhen(true)] out T data);
        public abstract bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data);

        [Serializable, NetSerializable]
        protected class AppearanceComponentState : ComponentState
        {
            public readonly Dictionary<object, object> Data;

            public AppearanceComponentState(Dictionary<object, object> data)
            {
                Data = data;
            }
        }
    }
}
