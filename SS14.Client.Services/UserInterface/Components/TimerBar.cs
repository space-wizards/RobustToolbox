using SS14.Client.Interfaces.Resource;
using System;
using System.Diagnostics;
using System.Drawing;
using SFML.Window;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Timer_Bar : Progress_Bar
    {
        private Stopwatch stopwatch;

        public Timer_Bar(Size size, TimeSpan countdownTime, IResourceManager resourceManager)
            : base(size, resourceManager)
        {
            stopwatch = new Stopwatch();
            max = (float) Math.Round(countdownTime.TotalSeconds);
            stopwatch.Restart();
            Update(0);
        }

        public override float Value
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); }
        }

        public override void Update(float frameTime)
        {
            if (stopwatch != null)
            {
                if (stopwatch.Elapsed.Seconds > max)
                    return;

                Value = stopwatch.Elapsed.Seconds;
                Text.Text =
                    DateTime.Now.AddSeconds(max - stopwatch.Elapsed.Seconds).Subtract(DateTime.Now).ToString(@"mm\:ss");
            }

            Text.Position = new Vector2(Position.X + (Size.Width/2f - Text.Width/2f),
                                         Position.Y + (Size.Height/2f - Text.Height/2f));
            ClientArea = new Rectangle(Position, Size);
        }

        public override void Dispose()
        {
            stopwatch.Stop();
            stopwatch = null;
            Text = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}