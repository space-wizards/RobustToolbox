using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    public class VideoSettings
    {
        private static Styles WindowStyle;
        private static ContextSettings OpenGLSettings;
        private static VideoMode WindowSettings;
        private static uint RefreshRate;

        public VideoSettings()
        {
            WindowStyle    = Styles.Default; // Titlebar + Resize + Close
            WindowSettings = VideoMode.DesktopMode; 

            OpenGLSettings = new ContextSettings();
            RefreshRate    = 30;
          
        }


        internal VideoMode getVideoMode()
        {
            if (WindowSettings.IsValid())
                return WindowSettings;
            else
                return new VideoMode(800, 600);
            //TODO logmanager show that the windowsettings were invalid
        }

        internal Styles getWindowStyle()
        {
            return WindowStyle; 
        }

        public static void SetWindowSize(uint width, uint height)
        {
            WindowSettings.Height = height;
            WindowSettings.Width  = width;
        }

        public static void Fullscreen(bool active)
        {
            if (WindowSettings.IsValid())
                WindowStyle = Styles.Fullscreen;
            else
                WindowStyle = Styles.Default;
            // Logmanager Windowsettings are invalid
        }

        public static void SetRefreshRate(uint rate)
        {
            if ((rate >= 30) && (rate <= 144))
                RefreshRate = rate;
            else
                RefreshRate = 30;
                //Logmanager Rate is either above 144 or below 30

        }

    }
}
