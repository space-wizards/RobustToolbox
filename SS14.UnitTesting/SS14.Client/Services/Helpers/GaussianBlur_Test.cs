using System;
using NUnit.Framework;
using SS14.Shared.IoC;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using System.Windows.Forms;
using SFML.System;
using SS14.Client.Services.Helpers;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;
using Color = SFML.Graphics.Color;

namespace SS14.UnitTesting.SS14.Client.Services.Helpers
{
    [TestFixture]
    public class GaussianBlur_Test : SS14UnitTest
    {

        private IPlayerConfigurationManager _configurationManager;      
        private IResourceManager _resourceManager; 

        private GaussianBlur _gaussianBlur;
        private RenderImage preblur;
        private RenderImage blur;

        private FrameEventArgs _frameEvent;
        private EventArgs _frameEventArgs;
        private Clock clock;

        private SFML.Graphics.Sprite sprite;
        
        public GaussianBlur_Test()
        {  
            //PreTest Setup
            base.InitializeCluwneLib(1280, 720, false, 60);
        
            _resourceManager = base.GetResourceManager;
            _configurationManager = base.GetConfigurationManager;         
            clock = base.GetClock;
        }


        [Test]
        public void GaussianBlurRadius11_ShouldBlur()
        {

            preblur = new RenderImage("testGaussianBlur", 1280, 768);
            _gaussianBlur = new GaussianBlur(_resourceManager);
        
           
            _gaussianBlur.SetRadius(11);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size((int)preblur.Width, (int)preblur.Height));
            
       

            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(SFML.Graphics.Color.Black);
                CluwneLib.Screen.DispatchEvents();
               
                
                preblur.BeginDrawing(); // set temp as CRT (Current Render Target)
                //preblur.Clear();       //Clear 
                sprite = _resourceManager.GetSprite("flashlight_mask");
                sprite.Position = Vector2.Zero;
                sprite.Draw();
                preblur.EndDrawing();  // set previous rendertarget as CRT (screen in this case)
              
                

                //_gaussianBlur.PerformGaussianBlur(preblur); // blur rendertarget

           
                preblur.Blit(0,0, preblur.Width, preblur.Height,Color.White, BlitterSizeMode.Crop ); // draw blurred nosprite logo
             

                
                CluwneLib.Screen.Display();
                
            }    
       
        }

        [Test]
        public void GaussianBlurRadius9_ShouldBlur()
        {
            preblur = new RenderImage("testGaussianBlur", 1280, 768);
            blur = new RenderImage("testGaussianBlur1", 1280, 768);
            _gaussianBlur = new GaussianBlur(_resourceManager);

            _gaussianBlur.SetRadius(9);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size((int)preblur.Width, (int)preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(SFML.Graphics.Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear 
                _resourceManager.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
                preblur.EndDrawing();  // set previous rendertarget as CRT (screen in this case)



                _gaussianBlur.PerformGaussianBlur(preblur); // blur rendertarget


                preblur.Blit(0, 0, 1280, 768); // draw blurred nosprite logo



                CluwneLib.Screen.Display();

            }

        }

        [Test]
        public void GaussianBlurRadius7_ShouldBlur()
        {
            preblur = new RenderImage("testGaussianBlur", 1280, 768);
            blur = new RenderImage("testGaussianBlur1", 1280, 768);
            _gaussianBlur = new GaussianBlur(_resourceManager);

            _gaussianBlur.SetRadius(7);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size((int)preblur.Width, (int)preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(SFML.Graphics.Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear 
                _resourceManager.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
                preblur.EndDrawing();  // set previous rendertarget as CRT (screen in this case)



                _gaussianBlur.PerformGaussianBlur(preblur); // blur rendertarget


                preblur.Blit(0, 0, 1280, 768); // draw blurred nosprite logo



                CluwneLib.Screen.Display();

            }

        }

        [Test]
        public void GaussianBlurRadius5_ShouldBlur()
        {
            preblur = new RenderImage("testGaussianBlur", 1280, 768);
            blur = new RenderImage("testGaussianBlur1", 1280, 768);
            _gaussianBlur = new GaussianBlur(_resourceManager);

            _gaussianBlur.SetRadius(5);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size((int)preblur.Width, (int)preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(SFML.Graphics.Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear 
                _resourceManager.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
                preblur.EndDrawing();  // set previous rendertarget as CRT (screen in this case)



                _gaussianBlur.PerformGaussianBlur(preblur); // blur rendertarget


                preblur.Blit(0, 0, 1280, 768); // draw blurred nosprite logo



                CluwneLib.Screen.Display();

            }

        }

        [Test]
        public void GaussianBlurRadius3_ShouldBlur()
        {
            preblur = new RenderImage("testGaussianBlur", 1280, 768);
            blur = new RenderImage("testGaussianBlur1", 1280, 768);
            _gaussianBlur = new GaussianBlur(_resourceManager);

            _gaussianBlur.SetRadius(3);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Size((int)preblur.Width, (int)preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(SFML.Graphics.Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear 
                _resourceManager.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
                preblur.EndDrawing();  // set previous rendertarget as CRT (screen in this case)



                _gaussianBlur.PerformGaussianBlur(preblur); // blur rendertarget


                preblur.Blit(0, 0, 1280, 768); // draw blurred nosprite logo



                CluwneLib.Screen.Display();

            }

        }

    }

}

