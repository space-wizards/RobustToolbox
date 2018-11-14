using System;
using SS14.Client.GodotGlue;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Popup))]
    public class Popup : Control
    {
        public Popup() : base()
        {
        }

        public Popup(string name) : base()
        {
        }

        internal Popup(Godot.Popup control) : base(control)
        {
        }

        new private Godot.Popup SceneControl;

        public event Action OnPopupHide;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Popup();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Popup) control;
        }

        public void Open(UIBox2? box = null)
        {
            SceneControl.Popup_(box?.Convert());
        }

        public void OpenCentered()
        {
            SceneControl.PopupCentered();
        }

        public void OpenMinimum()
        {
            SceneControl.PopupCenteredMinsize();
        }

        private GodotSignalSubscriber0 __popupHideSubscriber;

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            __popupHideSubscriber = new GodotSignalSubscriber0();
            __popupHideSubscriber.Connect(SceneControl, "popup_hide");
            __popupHideSubscriber.Signal += __popupHideHook;
        }

        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            __popupHideSubscriber.Disconnect(SceneControl, "popup_hide");
            __popupHideSubscriber.Dispose();
            __popupHideSubscriber = null;
        }

        private void __popupHideHook()
        {
            OnPopupHide?.Invoke();
        }
    }
}
