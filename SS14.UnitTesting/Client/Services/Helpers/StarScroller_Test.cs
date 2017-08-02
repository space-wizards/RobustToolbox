using NUnit.Framework;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using System;

namespace SS14.UnitTesting.SS14.Client.Helpers
{
    [TestFixture, Explicit]
    [Category("rendering")]
    public class StarScroller : SS14UnitTest
    {
        public override bool NeedsClientConfig => true;
        public override bool NeedsResourcePack => true;
        public override UnitTestProject Project => UnitTestProject.Client;

        private RenderImage renderimage;

        private FrameEventArgs _frameEvent;
        private EventArgs _frameEventArgs;
        private Clock clock = new Clock();

        private StarScroller Stars;

        [Test]
        public void CreateStarScroller_ShouldCreateStars()
        {
            base.InitializeCluwneLib(1280, 720, false, 60);
            Stars = new StarScroller();
            renderimage = new RenderImage("StarScroller", 1920, 1080);

            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(SFML.Graphics.Color.Black);
                CluwneLib.Screen.DispatchEvents();

                renderimage.BeginDrawing(); // set temp as CRT (Current Render Target)
                renderimage.Clear();       //Clear
                base.GetResourceCache.GetSprite("AAAA").Draw(); //Draw NoSpritelogo

                renderimage.EndDrawing();  // set previous rendertarget as CRT (screen in this case)

                renderimage.Blit(0, 0, 1280, 768); // draw blurred nosprite logo

                CluwneLib.Screen.Display();
            }
        }
    }
}
