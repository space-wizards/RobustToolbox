using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber5 : BaseGodotSignalSubscriber
    {
        public event Action<object, object, object, object, object> Signal;

        public void SignalInvoker(object a, object b, object c, object d, object e)
        {
            Signal?.Invoke(a, b, c, d, e);
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
