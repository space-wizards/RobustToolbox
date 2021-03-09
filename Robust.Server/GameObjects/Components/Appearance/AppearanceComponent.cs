using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects
{
    public sealed class AppearanceComponent : SharedAppearanceComponent
    {
        [ViewVariables]
        readonly Dictionary<object, object> data = new();

        public override void SetData(string key, object value)
        {
            if (data.TryGetValue(key, out var existing) && existing.Equals(value))
                return;

            data[key] = value;
            Dirty();
        }

        public override void SetData(Enum key, object value)
        {
            if (data.TryGetValue(key, out var existing) && existing.Equals(value))
                return;

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

        public override bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data)
        {
            return TryGetData(key, out data);
        }

        public override bool TryGetData<T>(string key, [NotNullWhen(true)] out T data)
        {
            return TryGetData(key, out data);
        }

        private bool TryGetData<T>(object key, [NotNullWhen(true)] out T data)
        {
            if (this.data.TryGetValue(key, out var dat))
            {
                data = (T)dat;
                return true;
            }

            data = default!;
            return false;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new AppearanceComponentState(data);
        }
    }
}
