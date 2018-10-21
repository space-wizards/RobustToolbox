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

        new private Godot.Range SceneControl;

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Range) control;
        }

        public float GetAsRatio()
        {
            return GameController.OnGodot ? SceneControl.GetAsRatio() : default;
        }

        public float Page
        {
            get => GameController.OnGodot ? SceneControl.Page : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Page = value;
                }
            }
        }

        public float MaxValue
        {
            get => GameController.OnGodot ? SceneControl.MaxValue : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MaxValue = value;
                }
            }
        }

        public float MinValue
        {
            get => GameController.OnGodot ? SceneControl.MinValue : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.MinValue = value;
                }
            }
        }

        public float Value
        {
            get => GameController.OnGodot ? SceneControl.Value : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Value = value;
                }
            }
        }
    }
}
