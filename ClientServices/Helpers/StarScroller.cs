using System;
using System.Drawing;
using GorgonLibrary;

namespace ClientServices.Helpers
{
    public class StarScroller
    {
        private struct Star
        {
            /// <summary>
            /// Position of the star.
            /// </summary>
            public Vector2D Position;

            /// <summary>
            /// Magnitude of the star.
            /// </summary>
            public Color Magnitude;

            /// <summary>
            /// Vertical delta.
            /// </summary>
            public float VDelta;
        }

        private Star[,] _stars;
        private readonly Random _random = new Random();

        public StarScroller()
        {
            MakeStars();
        }

        private void MakeStars()
        {
            _stars = new Star[64, 4];

            for (var layer = 0; layer < 4; layer++)
            {
                for (var i = 0; i < _stars.Length / 4; i++)
                {
                    _stars[i, layer].Position = new Vector2D((float)(_random.NextDouble() * Gorgon.Screen.Width), (float)(_random.NextDouble() * Gorgon.Screen.Height));

                    // Select magnitude.
                    switch (layer)
                    {
                        case 0:
                            _stars[i, layer].Magnitude = Color.FromArgb(255, 255, 255);
                            _stars[i, layer].VDelta = (float)(_random.NextDouble() * 100.0) + 55.0f;
                            break;
                        case 1:
                            _stars[i, layer].Magnitude = Color.FromArgb(192, 192, 192);
                            _stars[i, layer].VDelta = (float)(_random.NextDouble() * 50.0) + 27.5f;
                            break;
                        case 2:
                            _stars[i, layer].Magnitude = Color.FromArgb(128, 128, 128);
                            _stars[i, layer].VDelta = (float)(_random.NextDouble() * 25.0) + 13.5f;
                            break;
                        default:
                            _stars[i, layer].Magnitude = Color.FromArgb(64, 64, 64);
                            _stars[i, layer].VDelta = (float)(_random.NextDouble() * 12.5) + 1.0f;
                            break;
                    }
                }
            }
        }

        private void DrawStars(int layer, float deltaTime)
        {
            // Draw the stars.
            for (var i = 0; i < _stars.Length / 4; i++)
            {
                Gorgon.Screen.SetPoint((int)_stars[i, layer].Position.X, (int)_stars[i, layer].Position.Y, _stars[i, layer].Magnitude);

                // Move the stars down.
                _stars[i, layer].Position.Y += _stars[i, layer].VDelta * deltaTime;

                // Wrap around.
                if (_stars[i, layer].Position.Y > Gorgon.Screen.Height)
                    _stars[i, layer].Position = new Vector2D((float)(_random.NextDouble() * Gorgon.Screen.Width), 0);
            }
        }

        public void Render(float xTopleft, float yTopleft)
        {
            Gorgon.Screen.Clear(Color.Black);
            DrawStars(3, (float)Gorgon.FrameStats.FrameDrawTime / 2000);
            DrawStars(2, (float)Gorgon.FrameStats.FrameDrawTime / 2000);
            for (int layer = 1; layer >= 0; layer--)
                DrawStars(layer, (float)Gorgon.FrameStats.FrameDrawTime / 2000);
        }


    }
}
