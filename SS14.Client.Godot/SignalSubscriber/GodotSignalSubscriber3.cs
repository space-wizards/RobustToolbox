using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber3 : BaseGodotSignalSubscriber
    {
        public event Action<object, object, object> Signal;

        public void SignalInvoker(object a, object b, object c)
        {
            try
            {
                Signal?.Invoke(a, b, c);
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
