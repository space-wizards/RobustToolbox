using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber1 : BaseGodotSignalSubscriber
    {
        public event Action<object> Signal;

        public void SignalInvoker(object a)
        {
            Signal?.Invoke(a);
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
