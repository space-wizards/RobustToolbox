using SFML.System;

namespace SS14.Client.Graphics.Timing
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
        private double  lastFrameTime;							// Last frame time.
        private double  frameDrawTime;							// Time to draw a frame in milliseconds.
        private double  lastFPSFrameTime;						// Last FPS.
        private double averageFps;						    // Average FPS.
        private double highestFps;							// Highest FPS.
        private double lowestFps;								// Lowest FPS.
        private double currentFps;							// Current FPS.
        private long    frameCount;							// Frame count.
        private long    totalFrameCount;                       // Total frame count.
        private Clock   timer;							        // FPS timer.
        private int     frameAvgCounter;						// Counter for frame average.
        private double  frameAvgSum;
        private double CurrentFrameTime;
            // Frame average sum.
        #endregion

        #region Properties
        public double FrameDrawTime
        {
            get { return frameDrawTime; }
        }

        public Clock Timer
        {
            get { return timer; }
            set { timer = value; }
        }

        public double AverageFps
        {
            get { return averageFps; }
        }

        public double HighestFps
        {
            get { return highestFps; }
        }

        public double LowestFps
        {
            get { return lowestFps; }
        }
        public double CurrentFps
        {
            get { return currentFps; }
        }

        public long FrameCount
        {
            get { return totalFrameCount; }
        }

        #endregion

        #region Constructor
        public TimingData(Clock sfmltimer)
        {
            timer = sfmltimer;
        }
        #endregion

        #region Methods

        private void GetFps()
        {
            
        }



        public void Reset()
        {
            if (timer != null)
                timer.Restart();
            lastFPSFrameTime = 0.0;
            currentFps = 0;
            frameCount = 0;
            lastFrameTime = 0.0;
            frameDrawTime = 0.0;
            frameAvgCounter = 0;
            frameAvgSum = 0.0;
            totalFrameCount = 0;
        }

        public void Update()
        {
            CurrentFrameTime = Timer.ElapsedTime.AsSeconds();
            Timer.Restart();
            lastFrameTime = CurrentFrameTime;
            currentFps = 1 / (CurrentFrameTime - lastFrameTime);

            

        }
        #endregion
    }
}
