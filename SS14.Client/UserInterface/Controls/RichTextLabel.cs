using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.RichTextLabel))]
    internal class RichTextLabel : Control
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
            get => GameController.OnGodot ? (bool)SceneControl.Get("bbcode_enabled") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("bbcode_enabled", value);
                }
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("clear");
            }
        }

        public Godot.Error AppendBBCode(string code)
        {
            if (GameController.OnGodot)
            {
                return (Godot.Error)SceneControl.Call("append_bbcode", code);
            }
            else
            {
                return default;
            }
        }

        public void PushColor(Color color)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("push_color", color.Convert());
            }
        }

        public void AddText(string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("add_text", text);
            }
        }

        public void Pop()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("pop");
            }
        }

        public void NewLine()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("newline");
            }
        }

        public bool ScrollFollowing
        {
            get => GameController.OnGodot ? (bool)SceneControl.Call("is_scroll_following") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Call("set_scroll_follow", value);
                }
            }
        }

        public void RemoveLine(int line)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("remove_line", line);
            }
        }
    }
}
