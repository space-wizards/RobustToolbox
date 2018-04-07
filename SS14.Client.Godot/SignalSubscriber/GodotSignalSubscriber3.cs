using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber3 : BaseGodotSignalSubscriber
    {
        public event Action<object, object, object> Signal;

        public void SignalInvoker(object a, object b, object c)
        {
            Signal?.Invoke(a, b, c);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Signal = null;
            }
        }
    }
}
