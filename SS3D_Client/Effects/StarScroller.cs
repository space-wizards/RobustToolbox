using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using SS3D.Modules;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace SS3D.Effects
{
    public class StarScroller
    {
        private SS3D.Effects.StarScroller.Star[,] _stars;
        private Random _rnd = new Random();

        public StarScroller()
        {
            MakeStars();
        }

        public struct Star
        {
            /// <summary>
            /// Position of the star.
            /// </summary>
            public Vector2D Position;
            /// <summary>
            /// Magnitude of the star.
            /// </summary>
            public System.Drawing.Color Magnitude;
            /// <summary>
            /// Vertical delta.
            /// </summary>
            public float VDelta;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="position">Position of the star.</param>
            /// <param name="magnitude">Magnitude of the star.</param>
            public Star(Vector2D position, System.Drawing.Color magnitude)
            {
                Position = position;
                Magnitude = magnitude;
                VDelta = 0;
            }
        }

        private void MakeStars()
        {
            _stars = new Star[64, 4];

            for (int layer = 0; layer < 4; layer++)
            {
                for (int i = 0; i < _stars.Length / 4; i++)
                {
                    _stars[i, layer].Position = new Vector2D((float)(_rnd.NextDouble() * Gorgon.Screen.Width), (float)(_rnd.NextDouble() * Gorgon.Screen.Height));

                    // Select magnitude.
                    switch (layer)
                    {
                        case 0:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(255, 255, 255);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 100.0) + 55.0f;
                            break;
                        case 1:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(192, 192, 192);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 50.0) + 27.5f;
                            break;
                        case 2:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(128, 128, 128);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 25.0) + 13.5f;
                            break;
                        default:
                            _stars[i, layer].Magnitude = System.Drawing.Color.FromArgb(64, 64, 64);
                            _stars[i, layer].VDelta = (float)(_rnd.NextDouble() * 12.5) + 1.0f;
                            break;
                    }
                }
            }
        }

        private void DrawStars(int layer, float deltaTime, float xTopleft, float yTopLeft)
        {
            // Draw the stars.
            for (int i = 0; i < _stars.Length / 4; i++)
            {
                Gorgon.Screen.SetPoint((int)_stars[i, layer].Position.X, (int)_stars[i, layer].Position.Y, _stars[i, layer].Magnitude);

                // Move the stars down.
                _stars[i, layer].Position.Y += _stars[i, layer].VDelta * deltaTime;

                // Wrap around.
                if (_stars[i, layer].Position.Y > Gorgon.Screen.Height)
                    _stars[i, layer].Position = new Vector2D((float)(_rnd.NextDouble() * Gorgon.Screen.Width), 0);
            }
        }

        public void Render(float xTopleft, float yTopleft)
        {
            Gorgon.Screen.Clear(System.Drawing.Color.Black);
            DrawStars(3, (float)Gorgon.FrameStats.FrameDrawTime / 2000, xTopleft, yTopleft);
            DrawStars(2, (float)Gorgon.FrameStats.FrameDrawTime / 2000, xTopleft, yTopleft);
            for (int layer = 1; layer >= 0; layer--)
                DrawStars(layer, (float)Gorgon.FrameStats.FrameDrawTime / 2000, xTopleft, yTopleft);
        }


    }
}
