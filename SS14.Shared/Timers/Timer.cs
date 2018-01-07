using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Timers
{
    public class Timer
    {
        /// <summary>
        /// Counts the time (in milliseconds) before firing again.
        /// </summary>
        private int _timeCounter { get; set; }

        /// <summary>
        /// Time (in milliseconds) between firings.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// Whether or not this timer should repeat.
        /// </summary>
        public bool IsRepeating { get; set; }

        /// <summary>
        /// Whether or not this timer is still running.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Called when the timer is fired.
        /// </summary>
        public event EventHandler Fired;

        public Timer(int time, bool isRepeating)
        {
            Time = time;
            IsRepeating = isRepeating;
            _timeCounter = Time;
            IsActive = true;
        }

        public void Update(float frameTime)
        {
            if (IsActive)
            {
                _timeCounter -= (int)Math.Floor(frameTime * 1000);

                if (_timeCounter <= 0)
                {
                    Fired(this, new EventArgs());

                    if (IsRepeating)
                    {
                        _timeCounter = Time;
                    }
                    else
                    {
                        IsActive = false;
                    }
                }
            }
        }
    }
}
