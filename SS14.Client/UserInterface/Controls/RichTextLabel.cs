using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    public class RichTextLabel : Control
    {
        public RichTextLabel() : base()
        {
        }
        public RichTextLabel(string name) : base(name)
        {
        }
        public RichTextLabel(Godot.RichTextLabel button) : base(button)
        {
        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.RichTextLabel();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);

            SceneControl = (Godot.RichTextLabel)control;
        }

        new private Godot.RichTextLabel SceneControl;

        public bool BBCodeEnabled
        {
            get => SceneControl.BbcodeEnabled;
            set => SceneControl.BbcodeEnabled = value;
        }

        public void Clear()
        {
            SceneControl.Clear();
        }

        public int AppendBBCode(string code)
        {
            return SceneControl.AppendBbcode(code);
        }

        public void PushColor(Color color)
        {
            SceneControl.PushColor(color.Convert());
        }

        public void AddText(string text)
        {
            SceneControl.AddText(text);
        }

        public void Pop()
        {
            SceneControl.Pop();
        }

        public void NewLine()
        {
            SceneControl.Newline();
        }

        public bool ScrollFollowing
        {
            get => SceneControl.IsScrollFollowing();
            set => SceneControl.SetScrollFollow(value);
        }
    }
}
