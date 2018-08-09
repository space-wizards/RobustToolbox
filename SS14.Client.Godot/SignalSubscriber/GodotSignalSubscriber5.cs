using System;

namespace SS14.Client.GodotGlue
{
    public class GodotSignalSubscriber5 : BaseGodotSignalSubscriber
    {
        public event Action<object, object, object, object, object> Signal;

        public void SignalInvoker(object a, object b, object c, object d, object e)
        {
            try
            {
                Signal?.Invoke(a, b, c, d, e);
            }
            catch (Exception ex)
            {
                HandleException(ex);
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
