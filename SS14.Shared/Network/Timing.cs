using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Network
{
    public class Timing
    {

        public List<float> FrameTimes;

        private static Stopwatch _sysTimer;
        private Stopwatch _realTime;


        public Timing()
        {
            if(_sysTimer == null)
            {
                _sysTimer = new Stopwatch();
                _sysTimer.Start();
            }

            _realTime = new Stopwatch();
        }


        /// <summary>
        /// Is the simulation currently paused?
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// How fast time passes in the simulation compared to RealTime. 1.0 = 100%, 0.25 = 25% (slow mo).
        /// Minimum timescale is 0.1, max is 2.0.
        /// </summary>
        public double TimeScale { get; set; }


        /// <summary>
        /// The current synchronized uptime of the simulation. Use this for in-game timing. This can be rewound for 
        /// prediction, and is affected by Paused and TimeScale.
        /// </summary>
        public double CurTime { get; set; }

        /// <summary>
        /// The current real uptime of the simulation. Use this for UI and out of game timing.
        /// </summary>
        public double RealTime { get; set; }

        /// <summary>
        /// High accuracy real time (milliseconds) since the program started. Use this for profiling.
        /// </summary>
        public double SysTime => _sysTimer.Elapsed.TotalMilliseconds;


        /// <summary>
        /// The simulated time (milliseconds) it took to render the last frame.
        /// </summary>
        public double FrameTime { get; set; }

        /// <summary>
        /// The real time (milliseconds) it took to render the last frame.
        /// </summary>
        public double RealFrameTime { get; set; }


        /// <summary>
        /// The current simulation tick being processed.
        /// </summary>
        public int CurTick { get; set; }
        
        /// <summary>
        /// The current real tickrate of the simulation.
        /// </summary>
        public int TickRate { get; set; }

        public void RealTimeRestart()
        {
            _realTime.Restart();
        }
    }
}
