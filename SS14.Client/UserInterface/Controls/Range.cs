using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace SS14.Client.UserInterface
{
    public abstract class Range : Control
    {
        public Range() : base()
        {
        }
        public Range(string name) : base(name)
        {
        }
        public Range(Godot.Range control) : base(control)
        {
        }

        new private Godot.Range SceneControl;

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Range)control;
        }

        public float GetAsRatio()
        {
            return SceneControl.GetAsRatio();
        }

        public float Page
        {
            get => SceneControl.Page;
            set => SceneControl.Page = value;
        }

        public float MaxValue
        {
            get => SceneControl.MaxValue;
            set => SceneControl.MaxValue = value;
        }

        public float MinValue
        {
            get => SceneControl.MinValue;
            set => SceneControl.MinValue = value;
        }

        public float Value
        {
            get => SceneControl.Value;
            set => SceneControl.Value = value;
        }
    }
}
