using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace SS14.Client.UserInterface.Controls
{
    public class Button : BaseButton
    {
        public Button() : base()
        {
        }
        public Button(string name) : base(name)
        {
        }
        public Button(Godot.Button button) : base(button)
        {
        }

        new private Godot.Button SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Button();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Button)control;
        }

        public AlignMode TextAlign
        {
            get => (AlignMode)SceneControl.Align;
            set => SceneControl.Align = (int)value;
        }

        public bool ClipText
        {
            get => SceneControl.ClipText;
            set => SceneControl.ClipText = value;
        }

        public bool Flat
        {
            get => SceneControl.Flat;
            set => SceneControl.Flat = value;
        }

        public string Text
        {
            get => SceneControl.Text;
            set => SceneControl.Text = value;
        }

        public enum AlignMode
        {
            Left = Godot.Button.ALIGN_LEFT,
            Center = Godot.Button.ALIGN_CENTER,
            Right = Godot.Button.ALIGN_RIGHT,
        }
    }
}
