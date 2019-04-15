using System;

namespace Robust.Client.GodotGlue
{
    public class GodotSignalSubscriber2 : BaseGodotSignalSubscriber
    {
        public event Action<object, object> Signal;

        public void SignalInvoker(object a, object b)
        {
            try
            {
                Signal?.Invoke(a, b);
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
