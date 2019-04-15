using System;

// Godot *really* doesn't like it if you inherit Godot.Object outside the main project.
// in fact, it hard crashes.
// So we use these tiny dummies to register signals.
// It's not clean but do you have a better idea?
namespace Robust.Client.GodotGlue
{
    public abstract class BaseGodotSignalSubscriber : Godot.Reference
    {
        public void Connect(Godot.Object obj, string signal)
        {
            obj.Connect(signal, this, "SignalInvoker");
        }

        public void Disconnect(Godot.Object obj, string signal)
        {
            obj.Disconnect(signal, this, "SignalInvoker");
        }

        protected void HandleException(Exception exception)
        {
            SS14Loader.Instance.ExceptionCaught(exception);
        }
    }
}
