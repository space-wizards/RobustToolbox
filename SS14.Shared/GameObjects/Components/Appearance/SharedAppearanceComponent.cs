using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.GameObjects.Components.Appearance
{
    public abstract class SharedAppearanceComponent : Component
    {
        public override string Name => "appearance";
        public override uint? NetID => NetIDs.APPEARANCE;
        public override Type StateType => typeof(AppearanceComponentState);

        public abstract void SetData(string key, object value);
        public abstract void SetData(Enum key, object value);

        public abstract T GetData<T>(string key);
        public abstract T GetData<T>(Enum key);

        public abstract bool TryGetData<T>(string key, out T data);
        public abstract bool TryGetData<T>(Enum key, out T data);

        [Serializable, NetSerializable]
        public class AppearanceComponentState : ComponentState
        {
            public readonly Dictionary<object, object> Data;

            public AppearanceComponentState(Dictionary<object, object> data) : base(NetIDs.APPEARANCE)
            {
                Data = data;
            }
        }
    }
}
