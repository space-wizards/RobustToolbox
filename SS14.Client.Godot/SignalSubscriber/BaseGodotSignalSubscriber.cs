// Godot *really* doesn't like it if you inherit Godot.Object outside the main project.
// in fact, it hard crashes.
// So we use these tiny dummies to register signals.
// It's not clean but do you have a better idea?
namespace SS14.Client.GodotGlue
{
    public abstract class BaseGodotSignalSubscriber : Godot.Object
    {
        public void Connect(Godot.Object obj, string signal)
        {
            obj.Connect(signal, this, "SignalInvoker");
        }

        public void Disconnect(Godot.Object obj, string signal)
        {
            obj.Disconnect(signal, this, "SignalInvoker");
        }
    }
}
