using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using ClientResourceManager;
using System.Diagnostics;

namespace SS3D.UserInterface
{
    class Timer_Bar : Progress_Bar
    {
        public override float Value 
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); } 
        }

        private Stopwatch stopwatch;

        public Timer_Bar(Size _size, TimeSpan countdownTime)
            : base(_size)
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

            Text.Position = new Vector2D(position.X + (size.Width / 2f - Text.Width / 2f), position.Y + (size.Height / 2f - Text.Height / 2f));
            ClientArea = new Rectangle(this.position, size);
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
