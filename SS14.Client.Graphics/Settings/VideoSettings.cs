using SFML.Window;

namespace SS14.Client.Graphics.Settings
{
    public class VideoSettings
    {
        private ContextSettings _glContextSettings;
        private uint _refreshRate;
        private VideoMode _videoMode;
        private Styles _windowStyle;

        public VideoSettings()
        {
            _windowStyle = Styles.Default; // Titlebar + Resize + Close
            _videoMode = VideoMode.DesktopMode;

            _glContextSettings = new ContextSettings();
            _refreshRate = 30;
        }

        public VideoSettings(VideoMode mode)
        {
            _videoMode = mode;
        }

        /// <summary>
        ///     Returns a valid Video Mode
        /// </summary>
        /// <returns></returns>
        public VideoMode GetVideoMode()
        {
            if (_videoMode.IsValid())
                return _videoMode;
            return new VideoMode(800, 600);
        }

        /// <summary>
        ///     Returns the current windowStyle
        /// </summary>
        /// <returns></returns>
        public Styles GetWindowStyle()
        {
            return _windowStyle;
        }

        /// <summary>
        ///     Sets the Window Size
        /// </summary>
        /// <param name="width"> Width of the window</param>
        /// <param name="height">Height of the window </param>
        public void SetWindowSize(uint width, uint height)
        {
            _videoMode.Height = height;
            _videoMode.Width = width;
        }

        /// <summary>
        ///     Activates or deactivates fullscreen
        /// </summary>
        /// <param name="active"> true for fullscreen, false for no fullscreen</param>
        public void SetFullScreen(bool active)
        {
            if (active)
                if (_videoMode.IsValid())
                    _windowStyle = Styles.Fullscreen;
                else
                    _windowStyle = Styles.Default;
            else
                _windowStyle = Styles.Default;
        }

        /// <summary>
        ///     Sets the Refresh Rate of the game
        /// </summary>
        /// <param name="rate"> Refresh Rate</param>
        public void SetRefreshRate(uint rate)
        {
            if (rate >= 30 && rate <= 144)
                _refreshRate = rate;
            else
                _refreshRate = 30;
            //Logmanager Rate is either above 144 or below 30
        }

        /// <summary>
        ///     Checks if the VideoSettings are valid and will work on the current PC system
        /// </summary>
        /// <returns>True if not null and settings are valid  </returns>
        public bool IsValid()
        {
            if (!_windowStyle.Equals(null) && _videoMode.IsValid() && !_glContextSettings.Equals(null))
                return true;
            return false;
        }
    }
}
