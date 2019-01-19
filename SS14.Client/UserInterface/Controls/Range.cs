using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return GameController.OnGodot ? (float)SceneControl.Call("get_as_ratio") : default;
        }

        public float Page
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("page") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("page", value);
                }
            }
        }

        public float MaxValue
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("max_value") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("max_value", value);
                }
            }
        }

        public float MinValue
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("min_value") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("min_value", value);
                }
            }
        }

        public float Value
        {
            get => GameController.OnGodot ? (float)SceneControl.Get("value") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("value", value);
                }
            }
        }
    }
}
