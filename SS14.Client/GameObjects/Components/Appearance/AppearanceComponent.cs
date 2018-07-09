using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Appearance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.GameObjects
{
    public sealed class AppearanceComponent : SharedAppearanceComponent
    {
        private Dictionary<object, object> data = new Dictionary<object, object>();
        internal readonly List<AppearanceVisualizer> Visualizers = new List<AppearanceVisualizer>();

        public bool AppearanceDirty { get; internal set; } = false;

        public override void SetData(string key, object value)
        {
            SetData(key, value);
        }

        public override void SetData(Enum key, object value)
        {
            SetData(key, value);
        }

        public override T GetData<T>(string key)
        {
            return (T)data[key];
        }

        public override T GetData<T>(Enum key)
        {
            return (T)data[key];
        }

        internal T GetData<T>(object key)
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

        internal bool TryGetData<T>(object key, out T data)
        {
            if (this.data.TryGetValue(key, out var dat))
            {
                data = (T)dat;
                return true;
            }

            data = default(T);
            return false;
        }

        private void SetData(object key, object value)
        {
            data[key] = value;

            AppearanceDirty = true;
        }

        public override void HandleComponentState(ComponentState state)
        {
            var actualState = (AppearanceComponentState)state;
            data = actualState.Data;
        }

        internal abstract class AppearanceVisualizer
        {
        }

        internal class SpriteLayerToggle : AppearanceVisualizer
        {
            public readonly object Key;
            public readonly int SpriteLayer;

            public SpriteLayerToggle(AppearanceComponent master, object key, int spriteLayer) : base(master)
            {
                Key = key;
                SpriteLayer = spriteLayer;
            }
        }
    }
}
