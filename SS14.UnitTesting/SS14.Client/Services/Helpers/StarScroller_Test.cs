using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SS14.Client.Services.Helpers;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Event;
using SFML.System;
using SS14.Client.Graphics;
using System.Drawing;

namespace SS14.UnitTesting.SS14.Client.Services.Helpers
{
    [TestClass]
    public class StarScroller : SS14UnitTest
    {


    
        private RenderImage renderimage;


        private FrameEventArgs _frameEvent;
        private EventArgs _frameEventArgs;
        private Clock clock;

        private StarScroller Stars;



        [TestMethod]
        public void CreateStarScroller_ShouldCreateStars()
        {


            base.InitializeCluwneLib();
            Stars = new StarScroller();
            renderimage = new RenderImage("StarScroller", 1920, 1080);

            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();


                renderimage.BeginDrawing(); // set temp as CRT (Current Render Target)
                renderimage.Clear();       //Clear 
                base.GetResourceManager.GetSprite("AAAA").Draw(); //Draw NoSpritelogo
    
                renderimage.EndDrawing();  // set previous rendertarget as CRT (screen in this case)
                           



                renderimage.Blit(0, 0, 1280, 768); // draw blurred nosprite logo




                CluwneLib.Screen.Display();

            }    
        }
    }
}
