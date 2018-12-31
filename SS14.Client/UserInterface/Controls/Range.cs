using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Range))]
    public abstract class Range : Control
    {
        public Range() : base()
        {
        }
        public Range(string name) : base(name)
        {
        }
        internal Range(Godot.Range control) : base(control)
        {
        }

        public float GetAsRatio()
        {
            return (float)SceneControl.Call("get_as_ratio");
        }

        public float Page
        {
            get => (float)SceneControl.Get("page");
            set => SceneControl.Set("page", value);
        }

        public float MaxValue
        {
            get => (float)SceneControl.Get("max_value");
            set => SceneControl.Set("max_value", value);
        }

        public float MinValue
        {
            get => (float)SceneControl.Get("min_value");
            set => SceneControl.Set("min_value", value);
        }

        public float Value
        {
            get => (float)SceneControl.Get("value");
            set => SceneControl.Set("value", value);
        }
    }
}
