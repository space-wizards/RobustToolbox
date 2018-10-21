using System;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.RichTextLabel))]
    public class RichTextLabel : Control
    {
        public RichTextLabel() : base()
        {
        }

        public RichTextLabel(string name) : base(name)
        {
        }

        internal RichTextLabel(Godot.RichTextLabel button) : base(button)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.RichTextLabel();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);

            SceneControl = (Godot.RichTextLabel) control;
        }

        new private Godot.RichTextLabel SceneControl;

        public bool BBCodeEnabled
        {
            get => GameController.OnGodot ? SceneControl.BbcodeEnabled : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.BbcodeEnabled = value;
                }
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Clear();
            }
        }

        public void AppendBBCode(string code)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AppendBbcode(code);
            }
        }

        public void PushColor(Color color)
        {
            if (GameController.OnGodot)
            {
                SceneControl.PushColor(color.Convert());
            }
        }

        public void AddText(string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AddText(text);
            }
        }

        public void Pop()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Pop();
            }
        }

        public void NewLine()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Newline();
            }
        }

        public bool ScrollFollowing
        {
            get => GameController.OnGodot ? SceneControl.IsScrollFollowing() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetScrollFollow(value);
                }
            }
        }
    }
}
