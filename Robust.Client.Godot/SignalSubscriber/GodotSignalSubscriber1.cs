using System;

namespace Robust.Client.GodotGlue
{
    public class GodotSignalSubscriber1 : BaseGodotSignalSubscriber
    {
        public event Action<object> Signal;

        public void SignalInvoker(object a)
        {
            try
            {
                Signal?.Invoke(a);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
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
