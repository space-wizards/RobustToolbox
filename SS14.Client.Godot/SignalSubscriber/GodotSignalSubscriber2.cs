using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber2 : BaseGodotSignalSubscriber
    {
        public event Action<object, object> Signal;

        public void SignalInvoker(object a, object b)
        {
            Signal?.Invoke(a, b);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Signal = null;
            }
        }
    }
}
