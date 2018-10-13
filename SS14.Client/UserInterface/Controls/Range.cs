using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.Range))]
    #endif
    public abstract class Range : Control
    {
        public Range() : base()
        {
        }
        public Range(string name) : base(name)
        {
        }
        #if GODOT
        internal Range(Godot.Range control) : base(control)
        {
        }

        new private Godot.Range SceneControl;

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Range)control;
        }
        #endif

        public float GetAsRatio()
        {
            #if GODOT
            return SceneControl.GetAsRatio();
            #else
            return default;
#endif
        }

        public float Page
        {
            #if GODOT
            get => SceneControl.Page;
            set => SceneControl.Page = value;
            #else
            get => default;
            set { }
            #endif
        }

        public float MaxValue
        {
            #if GODOT
            get => SceneControl.MaxValue;
            set => SceneControl.MaxValue = value;
            #else
            get => default;
            set { }
            #endif
        }

        public float MinValue
        {
            #if GODOT
            get => SceneControl.MinValue;
            set => SceneControl.MinValue = value;
            #else
            get => default;
            set { }
            #endif
        }

        public float Value
        {
            #if GODOT
            get => SceneControl.Value;
            set => SceneControl.Value = value;
            #else
            get => default;
            set { }
            #endif
        }
    }
}
