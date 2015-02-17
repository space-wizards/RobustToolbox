using System;
using System.Windows.Forms;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.CluwneLib.Event;
using SS14.Client.Graphics.CluwneLib.Render;
using SS14.Client.Graphics.CluwneLib.Timing;

namespace SS14.Client.Graphics.CluwneLib
{
    public class CluwneLib
    {
        public static Viewport CurrentClippingViewport;
        private static Clock _timer;
        private static RenderTarget[] _currentTarget;
        public static event FrameEventHandler Idle;

        /// <summary>
        /// Start engine rendering.
        /// </summary>
        /// Shamelessly taken from Gorgon.
        public static void Go()
        {
            if (!IsInitialized)
                ; //TODO: Throw exception

            if ((Screen != null) && (_currentTarget == null))
                throw new InvalidOperationException("The render target is invalid.");

            if (IsRunning)
                return;

            _timer.Restart();
            FrameStats.Reset();

            if (_currentTarget != null)
            {
                for (int i = 0; i < _currentTarget.Length; i++)
                {
                    if (_currentTarget[i] != null)
                    {
                        //_currentTarget[i].Refresh(); TODO: Refresh viewport   
                    }
                }
                
            }

            Application.Idle += new EventHandler(Run);

            IsRunning = true;
        }

        public static CluwneWindow Screen { get; set; }

        public static bool IsRunning { get; set; }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();

            IsInitialized = true;

            _timer = new Clock();

            FrameStats = new TimingData(_timer);
        }

        private static void Terminate()
        {
            throw new NotImplementedException();
        }

        public static TimingData FrameStats { get; set; }

        public static bool IsInitialized { get; set; }
        public static RenderTarget CurrentRenderTarget 
        {
            get { return _currentTarget[0]; }
            set
            {
                if (value == null)
                    value = Screen;
                SetAdditionalRenderTarget(0, value);
            }
        }

        private static void SetAdditionalRenderTarget(int i, RenderTarget value)
        {
            throw new NotImplementedException();
        }

        public static RenderTarget GetAdditionalRenderTarget(int index)
        {
            return _currentTarget[index];
        }


        public static void Run(object sender, EventArgs e)
        {
            
        }

        public static void SetMode(Form mainWindow, int displayWidth, int displayHeight, bool b, bool b1, bool b2, int refresh)
        {
            throw new NotImplementedException(); //TOOD: Change bufferRgb888 to correct class.
        }
    }
}
