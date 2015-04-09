using SFML.System;

namespace SS14.Client.Graphics.CluwneLib.Timing
{
    /// <summary>
    /// Handles TimingData
    /// </summary>
    public class TimingData
    {
        #region Constants.
        private const int AverageFpsFrameMax = 5;       		// Maximum number of frames to skip before average is calculated.
        #endregion

        #region Variables.
        private double  _lastFrameTime;							// Last frame time.
        private double  _frameDrawTime;							// Time to draw a frame in milliseconds.
        private double  _lastFPSFrameTime;						// Last FPS.
        private float   _averageFps;						    // Average FPS.
        private float   _highestFps;							// Highest FPS.
        private float   _lowestFps;								// Lowest FPS.
        private float   _currentFps;							// Current FPS.
        private long    _frameCount;							// Frame count.
        private long    _totalFrameCount;                       // Total frame count.
        private Clock   _timer;							        // FPS timer.
        private int     _frameAvgCounter;						// Counter for frame average.
        private double  _frameAvgSum;							// Frame average sum.
        #endregion

        #region Properties
        public double FrameDrawTime
        {
            get { return _frameDrawTime; }
        }

        public Clock Timer
        {
            get { return _timer; }
            set { _timer = value; }
        }

        public float AverageFps
        {
            get { return _averageFps; }
        }

        public float HighestFps
        {
            get { return _highestFps; }
        }

        public float LowestFps
        {
            get { return _lowestFps; }
        }

        public float CurrentFps
        {
            get { return _currentFps; }
        }

        public long FrameCount
        {
            get { return _totalFrameCount; }
        }

        #endregion

        #region Constructor
        public TimingData(Clock timer)
        {
            _timer = timer;
        }
        #endregion

        #region Methods

        private void GetFps()
        {
            
        }



        public void Reset()
        {
            if (_timer != null)
                _timer.Restart();
            _lastFPSFrameTime = 0.0;
            _currentFps = 0.0f;
            _frameCount = 0;
            _lastFrameTime = 0.0;
            _frameDrawTime = 0.0;
            _frameAvgCounter = 0;
            _frameAvgSum = 0.0;
            _totalFrameCount = 0;
        }

        public bool Update()
        {
            return false;
        }
        #endregion
    }
}
