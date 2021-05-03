using System;
using System.Collections;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class FrameGraph : Control
    {
        private readonly IGameTiming _gameTiming;
        private readonly IConfigurationManager _cfg;

        /// <summary>
        ///     How many frames we show at once.
        /// </summary>
        private const int TrackedFrames = 120;

        /// <summary>
        ///     The width of a single frame on the bar chart, in pixels.
        /// </summary>
        private const int FrameWidth = 4;

        /// <summary>
        ///     The height of a single frame in pixels to the target <see cref="TargetFrameRate"/> FPS.
        ///     If the frame takes say 1/30 seconds to complete (30 FPS) it would be twice this value.
        /// </summary>
        private const int FrameHeight = 60;

        // We keep track of frame times in a ring buffer.
        private readonly float[] _frameTimes = new float[TrackedFrames];
        private readonly BitArray _gcMarkers = new(TrackedFrames);

        // Position of the last frame in the ring buffer.
        private int _frameIndex;
        private int _lastGCCount;

        public FrameGraph(IGameTiming gameTiming, IConfigurationManager cfg)
        {
            _gameTiming = gameTiming;
            _cfg = cfg;

            HorizontalAlignment = HAlignment.Left;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return (TrackedFrames * FrameWidth, FrameHeight * 2);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            var gcCount = GC.CollectionCount(0);

            _frameTimes[_frameIndex] = (float)_gameTiming.RealFrameTime.TotalSeconds;
            _gcMarkers[_frameIndex] = gcCount > _lastGCCount;

            _frameIndex = (_frameIndex + 1) % TrackedFrames;
            _lastGCCount = gcCount;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var target = _cfg.GetCVar(CVars.DebugTargetFps);

            Span<Vector2> triangle = stackalloc Vector2[3];

            float maxHeight = 0;
            for (var i = 0; i < _frameTimes.Length; i++)
            {
                var currentFrameIndex = MathHelper.Mod(_frameIndex - 1 - i, TrackedFrames);
                var frameTime = _frameTimes[currentFrameIndex];
                maxHeight = System.Math.Max(maxHeight, FrameHeight * (frameTime * target));
            }

            float ratio = maxHeight > PixelHeight ? PixelHeight / maxHeight : 1;
            for(int i = 0; i < _frameTimes.Length; i++)
            {
                var currentFrameIndex = MathHelper.Mod(_frameIndex - 1 - i, TrackedFrames);
                var frameTime = _frameTimes[currentFrameIndex];
                var frameHeight = FrameHeight * (frameTime * target);
                var x = FrameWidth * UIScale * (TrackedFrames - 1 - i);
                var rect = new UIBox2(x, PixelHeight - (frameHeight * ratio), x + FrameWidth * UIScale, PixelHeight);

                Color color;
                if (frameTime > 1f / (target / 2 - 1))
                {
                    color = Color.Red;
                }
                else if (frameTime > 1f / (target - 1))
                {
                    color = Color.Yellow;
                }
                else
                {
                    color = Color.Lime;
                }
                handle.DrawRect(rect, color);

                var gc = _gcMarkers[currentFrameIndex];
                if (gc)
                {
                    triangle[0] = (rect.Left, 0);
                    triangle[1] = (rect.Right, 0);
                    triangle[2] = (rect.Center.X, 5);

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, triangle, Color.LightBlue);
                }
            }
        }
    }
}
