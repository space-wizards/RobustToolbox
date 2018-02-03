using System;
using SS14.Client.Interfaces.Graphics.ClientEye;

namespace SS14.Client.Graphics.ClientEye
{
    public class EyeManager : IEyeManager, IDisposable
    {
        // We default to this when we get set to a null eye.
        private FixedEye defaultEye;

        private IEye currentEye;
        public IEye CurrentEye
        {
            get => currentEye;
            set
            {
                if (currentEye == value)
                {
                    return;
                }

                currentEye.GodotCamera.Current = false;
                if (value != null)
                {
                    currentEye = value;
                }
                else
                {
                    currentEye = defaultEye;
                }

                currentEye.GodotCamera.Current = true;
            }
        }

        public void Initialize()
        {
            defaultEye = new FixedEye();
            currentEye = defaultEye;
            currentEye.GodotCamera.Current = true;
        }

        public void Dispose()
        {
            defaultEye.Dispose();
        }
    }
}
