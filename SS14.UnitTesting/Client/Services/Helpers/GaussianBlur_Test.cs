using NUnit.Framework;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Helpers;
using SS14.Shared.Interfaces.Configuration;
using System;

namespace SS14.UnitTesting.SS14.Client.Helpers
{
    [TestFixture, Explicit]
    [Category("rendering")]
    public class GaussianBlur_Test : SS14UnitTest
    {
        public override bool NeedsClientConfig => true;
        public override bool NeedsResourcePack => true;
        public override UnitTestProject Project => UnitTestProject.Client;

        private IConfigurationManager _configurationManager;
        private IResourceCache _resourceCache;

        private GaussianBlur _gaussianBlur;
        private RenderImage preblur;
        private RenderImage blur;

        private FrameEventArgs _frameEvent;
        private Clock clock;

        private Sprite sprite;

        [OneTimeSetUp]
        public void Setup()
        {
            //PreTest Setup
            base.InitializeCluwneLib(1280, 720, false, 60);

            _resourceCache = base.GetResourceCache;
            _configurationManager = base.GetConfigurationManager;
            clock = base.GetClock;
        }


        [Test]
        public void GaussianBlurRadius11_ShouldBlur()
        {

            preblur = new RenderImage("testGaussianBlur", 1280, 768);
            _gaussianBlur = new GaussianBlur(_resourceCache);


            _gaussianBlur.SetRadius(11);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2(preblur.Width, preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT (Current Render Target)
                //preblur.Clear();       //Clear
                sprite = _resourceCache.GetSprite("flashlight_mask");
                sprite.Position = new Vector2();
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
            _gaussianBlur = new GaussianBlur(_resourceCache);

            _gaussianBlur.SetRadius(9);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2(preblur.Width, preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear
                _resourceCache.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
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
            _gaussianBlur = new GaussianBlur(_resourceCache);

            _gaussianBlur.SetRadius(7);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2(preblur.Width, preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear
                _resourceCache.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
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
            _gaussianBlur = new GaussianBlur(_resourceCache);

            _gaussianBlur.SetRadius(5);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2(preblur.Width, preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear
                _resourceCache.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
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
            _gaussianBlur = new GaussianBlur(_resourceCache);

            _gaussianBlur.SetRadius(3);
            _gaussianBlur.SetAmount(2);
            _gaussianBlur.SetSize(new Vector2(preblur.Width, preblur.Height));



            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = clock.ElapsedTime.AsSeconds();
                clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();


                preblur.BeginDrawing(); // set temp as CRT
                preblur.Clear();       //Clear
                _resourceCache.GetSprite("flashlight_mask").Draw(); //Draw NoSpritelogo
                preblur.EndDrawing();  // set previous rendertarget as CRT (screen in this case)



                _gaussianBlur.PerformGaussianBlur(preblur); // blur rendertarget


                preblur.Blit(0, 0, 1280, 768); // draw blurred nosprite logo



                CluwneLib.Screen.Display();

            }

        }

    }

}
