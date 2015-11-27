using SFML.Window;


namespace SS14.Client.Graphics
{
    public class VideoSettings
    {
        private Styles WindowStyle;
        private ContextSettings OpenGLSettings;
        private VideoMode WindowSettings;
        private uint RefreshRate;

        public VideoSettings()
        {
            WindowStyle    = Styles.Default; // Titlebar + Resize + Close
            WindowSettings = VideoMode.DesktopMode; 

            OpenGLSettings = new ContextSettings();
            RefreshRate    = 30;          
        }

        /// <summary>
        /// Returns a valid Video Mode
        /// </summary>
        /// <returns></returns>
        public VideoMode getVideoMode()
        {
            if (WindowSettings.IsValid())
                return WindowSettings;
            else
                return new VideoMode(800, 600);
            //TODO logmanager show that the windowsettings were invalid
        }

        /// <summary>
        /// Returns the current windowStyle
        /// </summary>
        /// <returns></returns>
        public Styles getWindowStyle()
        {
            return WindowStyle; 
        }

        /// <summary>
        /// Sets the Window Size
        /// </summary>
        /// <param name="width"> Width of the window</param>
        /// <param name="height">Height of the window </param>
        public void SetWindowSize(uint width, uint height)
        {
            WindowSettings.Height = height;
            WindowSettings.Width  = width;
        }

        /// <summary>
        /// Activates or deactivates fullscreen 
        /// </summary>
        /// <param name="active"> true for fullscreen, false for no fullscreen</param>
        public void SetFullscreen(bool active)
        {
            if (active)
            {
                if (WindowSettings.IsValid())
                    WindowStyle = Styles.Fullscreen;
                else
                    WindowStyle = Styles.Default;        
            }
            else 
                 WindowStyle = Styles.Default;               
                // Logmanager Windowsettings are invalid
        }

        /// <summary>
        /// Sets the Refresh Rate of the game
        /// </summary>
        /// <param name="rate"> Refresh Rate</param>
        public void SetRefreshRate(uint rate)
        {
            if ((rate >= 30) && (rate <= 144))
                RefreshRate = rate;
            else
                RefreshRate = 30;
           //Logmanager Rate is either above 144 or below 30

        }

        /// <summary>
        /// Checks if the VideoSettings are valid and will work on the current PC system
        /// </summary>
        /// <returns>True if not null and settings are valid  </returns>
        public bool IsValid()
        {
            if (!WindowStyle.Equals(null) && WindowSettings.IsValid() && !OpenGLSettings.Equals(null))
            {
                return true;
            }
            else return false;
        }

    }
}
