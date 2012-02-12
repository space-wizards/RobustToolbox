using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using System.Diagnostics;

namespace ClientServices.UserInterface.Components
{
    class Timer_Bar : Progress_Bar
    {
        public override float Value 
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); } 
        }

        private Stopwatch stopwatch;

        public Timer_Bar(Size size, TimeSpan countdownTime, IResourceManager resourceManager)
            : base(size, resourceManager)
        {
            stopwatch = new Stopwatch();
            max = (float)Math.Round(countdownTime.TotalSeconds);
            stopwatch.Restart();
            Update();
        }

        public override void Update()
        {
            if (stopwatch != null)
            {
                if (stopwatch.Elapsed.Seconds > max)
                    return;

                Value = stopwatch.Elapsed.Seconds;
                Text.Text = DateTime.Now.AddSeconds(max - stopwatch.Elapsed.Seconds).Subtract(DateTime.Now).ToString(@"mm\:ss");
            }

            Text.Position = new Vector2D(Position.X + (Size.Width / 2f - Text.Width / 2f), Position.Y + (Size.Height / 2f - Text.Height / 2f));
            ClientArea = new Rectangle(this.Position, Size);
        }

        public override void Dispose()
        {
            stopwatch.Stop();
            stopwatch = null;
            Text = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
