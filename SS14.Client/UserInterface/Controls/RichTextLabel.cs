using System;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.RichTextLabel))]
    #endif
    public class RichTextLabel : Control
    {
        public RichTextLabel() : base()
        {
        }
        public RichTextLabel(string name) : base(name)
        {
        }
        #if GODOT
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

            SceneControl = (Godot.RichTextLabel)control;
        }

        new private Godot.RichTextLabel SceneControl;
        #endif

        public bool BBCodeEnabled
        {
            #if GODOT
            get => SceneControl.BbcodeEnabled;
            set => SceneControl.BbcodeEnabled = value;
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public void Clear()
        {
            #if GODOT
            SceneControl.Clear();
            #endif
        }

        public void AppendBBCode(string code)
        {
            #if GODOT
            SceneControl.AppendBbcode(code);
            #endif
        }

        public void PushColor(Color color)
        {
            #if GODOT
            SceneControl.PushColor(color.Convert());
            #endif
        }

        public void AddText(string text)
        {
            #if GODOT
            SceneControl.AddText(text);
            #endif
        }

        public void Pop()
        {
            #if GODOT
            SceneControl.Pop();
            #endif
        }

        public void NewLine()
        {
            #if GODOT
            SceneControl.Newline();
            #endif
        }

        public bool ScrollFollowing
        {
            #if GODOT
            get => SceneControl.IsScrollFollowing();
            set => SceneControl.SetScrollFollow(value);
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }
    }
}
