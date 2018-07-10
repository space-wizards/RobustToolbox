using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Appearance;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public sealed class AppearanceComponent : SharedAppearanceComponent
    {
        readonly Dictionary<object, object> data = new Dictionary<object, object>();

        public override void SetData(string key, object value)
        {
            data[key] = value;
            Dirty();
        }

        public override void SetData(Enum key, object value)
        {
            data[key] = value;
            Dirty();
        }

        public override T GetData<T>(string key)
        {
            return (T)data[key];
        }

        public override T GetData<T>(Enum key)
        {
            return (T)data[key];
        }

        public override bool TryGetData<T>(Enum key, out T data)
        {
            return TryGetData(key, out data);
        }

        public override bool TryGetData<T>(string key, out T data)
        {
            return TryGetData(key, out data);
        }

        bool TryGetData<T>(object key, out T data)
        {
            if (this.data.TryGetValue(key, out var dat))
            {
                data = (T)dat;
                return true;
            }

            data = default(T);
            return false;
        }

        public override ComponentState GetComponentState()
        {
            return new AppearanceComponentState(data);
        }
    }
}
