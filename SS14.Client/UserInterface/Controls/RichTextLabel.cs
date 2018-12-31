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

        public bool BBCodeEnabled
        {
            get => (bool)SceneControl.Get("bbcode_enabled");
            set => SceneControl.Set("bbcode_enabled", value);
        }

        public void Clear()
        {
            SceneControl.Call("clear");
        }

        public Godot.Error AppendBBCode(string code)
        {
            return (Godot.Error)SceneControl.Call("append_bbcode", code);
        }

        public void PushColor(Color color)
        {
            SceneControl.Call("push_color", color.Convert());
        }

        public void AddText(string text)
        {
            SceneControl.Call("add_text", text);
        }

        public void Pop()
        {
            SceneControl.Call("pop");
        }

        public void NewLine()
        {
            SceneControl.Call("newline");
        }

        public bool ScrollFollowing
        {
            get => (bool)SceneControl.Call("is_scroll_following");
            set => SceneControl.Call("set_scroll_following", value);
        }
    }
}
