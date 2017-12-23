using SS14.Client.GodotGlue;
using SS14.Client.Input;

namespace SS14.Client.UserInterface
{
    // Signal registration is a lot of annoying, hacky and ugly boiler plate
    // so we put it in here!
    public partial class Control
    {
        private GodotSignalSubscriber0 __mouseEnteredSubscriber;
        private GodotSignalSubscriber0 __mouseExitedSubscriber;
        private GodotSignalSubscriber1 __guiInputSubscriber;
        private GodotSignalSubscriber0 __focusEnteredSubscriber;
        private GodotSignalSubscriber0 __focusExitedSubscriber;

        protected virtual void SetupSignalHooks()
        {
            __mouseEnteredSubscriber = new GodotSignalSubscriber0();
            __mouseEnteredSubscriber.Connect(SceneControl, "mouse_entered");
            __mouseEnteredSubscriber.Signal += __mouseEnteredHook;

            __mouseExitedSubscriber = new GodotSignalSubscriber0();
            __mouseExitedSubscriber.Connect(SceneControl, "mouse_exited");
            __mouseExitedSubscriber.Signal += __mouseExitedHook;

            __guiInputSubscriber = new GodotSignalSubscriber1();
            __guiInputSubscriber.Connect(SceneControl, "gui_input");
            __guiInputSubscriber.Signal += __guiInputHook;

            __focusEnteredSubscriber = new GodotSignalSubscriber0();
            __focusEnteredSubscriber.Connect(SceneControl, "focus_entered");
            __focusEnteredSubscriber.Signal += __focusEnteredHook;

            __focusExitedSubscriber = new GodotSignalSubscriber0();
            __focusExitedSubscriber.Connect(SceneControl, "focus_exited");
            __focusExitedSubscriber.Signal += __focusExitedHook;
        }

        protected virtual void DisposeSignalHooks()
        {
            __mouseEnteredSubscriber.Disconnect(SceneControl, "mouse_entered");
            __mouseEnteredSubscriber.Dispose();
            __mouseEnteredSubscriber = null;

            __mouseExitedSubscriber.Disconnect(SceneControl, "mouse_exited");
            __mouseExitedSubscriber.Dispose();
            __mouseExitedSubscriber = null;

            __guiInputSubscriber.Disconnect(SceneControl, "gui_input");
            __guiInputSubscriber.Dispose();
            __guiInputSubscriber = null;

            __focusEnteredSubscriber.Disconnect(SceneControl, "focus_entered");
            __focusEnteredSubscriber.Dispose();
            __focusEnteredSubscriber = null;

            __focusExitedSubscriber.Disconnect(SceneControl, "focus_exited");
            __focusExitedSubscriber.Dispose();
            __focusExitedSubscriber = null;
        }

        private void __mouseEnteredHook()
        {
            MouseEntered();
        }

        private void __mouseExitedHook()
        {
            MouseExited();
        }

        private void __guiInputHook(object ev)
        {
            HandleGuiInput((Godot.InputEvent)ev);
        }

        private void __focusEnteredHook()
        {
            FocusEntered();
        }

        private void __focusExitedHook()
        {
            FocusExited();
        }
    }
}
