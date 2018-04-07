using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber4 : BaseGodotSignalSubscriber
    {
        public event Action<object, object, object, object> Signal;

        public void SignalInvoker(object a, object b, object c, object d)
        {
            Signal?.Invoke(a, b, c, d);
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
