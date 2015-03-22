using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.State.States;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SFML.Graphics;
using SFML.Window;
using SS14.Client.Graphics.CluwneLib;
using SS14.Client.Graphics.CluwneLib.Event;
using SS14.Client.Graphics.CluwneLib.Render;
using Color = System.Drawing.Color;
using KeyArgs = SFML.Window.KeyEventArgs;
using SS14.Client.Graphics.CluwneLib.Sprite;



namespace SS14.Client
{
    public class GameController 
    {


        #region Fields

        private IConfigurationManager _configurationManager;
      //  private Input _input;
        private INetworkGrapher _netGrapher;
        private INetworkManager _networkManager;
        private IStateManager _stateManager;
        private IUserInterfaceManager _userInterfaceManager;
        private IResourceManager _resourceManager;

        #endregion

        #region Properties

        #endregion

        #region Methods

        #region Constructors

        public GameController()
        {
            _configurationManager = IoCManager.Resolve<IConfigurationManager>();
            IoCManager.Resolve<IConfigurationManager>().Initialize("./config.xml");

            _resourceManager = IoCManager.Resolve<IResourceManager>();

            _resourceManager.LoadBaseResources();
            _resourceManager.LoadLocalResources();
          

            //Initialization of private members
           
            _networkManager = IoCManager.Resolve<INetworkManager>();
            _netGrapher = IoCManager.Resolve<INetworkGrapher>();
            _stateManager = IoCManager.Resolve<IStateManager>();
            _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

        
           

            //Setup
            SetupCluwne();
            SetupInput();

         
            //Testing! 
            TextSprite test = new TextSprite("TEST", "Narsietext", _resourceManager.GetFont("CALIBRI"));
            test.Position = new Shared.Maths.Vector2(500, 640);
            test.Color = Color.DarkRed;
            test.Text = "NAR-SIE HAS RISEN";

            TextSprite test4 = new TextSprite("TEST", "Narsietext", _resourceManager.GetFont("CALIBRI"));
            test4.Position = new Shared.Maths.Vector2(600, 600);
            test4.Color = Color.Gold;
            test4.Text = "SFML Rendering Engine";

            TextSprite test5 = new TextSprite("TEST", "Narsietext", _resourceManager.GetFont("CALIBRI"));
            test5.Position = new Shared.Maths.Vector2(700, 540);
            test5.Color = Color.Gold;
            test5.Text = "Project == notDead";


            CluwneLib.drawHollowRectangle(100, 100, 100, 100, .6f, Color.Blue);
            CluwneLib.drawPoint(134, 223, Color.Beige);
            CluwneLib.drawCircle(121, 142, 20, Color.Crimson);

            
            Texture temp = new Texture(_resourceManager.GetImage("73f"));
          

            Sprite test3 = new Sprite();
            test3.Texture = temp;
           


            CluwneLib.CurrentRenderTarget.Draw(test3);



            test.Draw();
            test4.Draw();
            test5.Draw();
            CluwneLib.Screen.Display();

            

             while(CluwneLib.Screen.IsOpen == true)
                {
                                    
                    
             
               
                    CluwneLib.Screen.WaitAndDispatchEvents();
                }

          

        }

        #endregion


       
        #region EventHandlers

        private void CluwneLibIdle(object sender, FrameEventArgs e)
        {
            _networkManager.UpdateNetwork();
            _stateManager.Update(e);

            _userInterfaceManager.Update(e.FrameDeltaTime);
            _userInterfaceManager.Render();

            _netGrapher.Update();
        }

        private void MainWindowLoad(object sender, EventArgs e)
        {
           
            _stateManager.RequestStateChange<MainScreen>();
        }

        private void MainWindowResizeEnd(object sender, EventArgs e)
        {
          

            _stateManager.CurrentState.FormResize();
        }

        #region Input Handling

        /// <summary>
        /// Handles any keydown events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.KeyboardInputEventArgs"/> instance containing the event data.</param>
        private void KeyDownEvent(object sender, KeyArgs e)
        {
            if(_stateManager!=null)
                _stateManager.KeyDown(e);

            switch (e.Code)
            {
                case Keyboard.Key.F3:
                    IoCManager.Resolve<INetworkGrapher>().Toggle();
                    break;
            }
        }

        /// <summary>
        /// Handles any keyup events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.KeyboardInputEventArgs"/> instance containing the event data.</param>
        private void KeyUpEvent(object sender, KeyArgs e)
        {
            if (_stateManager != null) 
                _stateManager.KeyUp(e);
        }

        /// <summary>
        /// Handles mouse wheel input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseWheelMoveEvent(object sender, MouseWheelEventArgs e)
        {
            if (_stateManager != null) 
                _stateManager.MouseWheelMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseMoveEvent(object sender, MouseMoveEventArgs e)
        {
            if (_stateManager != null) 
                _stateManager.MouseMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseDownEvent(object sender, MouseButtonEventArgs e)
        {
            if (_stateManager != null) 
                _stateManager.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GorgonLibrary.InputDevices.MouseInputEventArgs"/> instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseButtonEventArgs e)
        {
            if (_stateManager != null) 
                _stateManager.MouseUp(e);
        }

        #endregion

        #endregion

        #region Privates

//        private void SetupGorgon()
//        {
//            uint displayWidth = _configurationManager.GetDisplayWidth();
//            uint displayHeight = _configurationManager.GetDisplayHeight();
//            bool fullscreen = _configurationManager.GetFullscreen();
//            var refresh = (int) _configurationManager.GetDisplayRefresh();
//            Size = new Size((int) displayWidth, (int) displayHeight);
//
//            //TODO. Find first compatible videomode and set it if no configuration is present. Else the client might crash due to invalid videomodes on the first start.
//
//            CluwneLib.Initialize();
//            //Gorgon.SetMode(this);
//            CluwneLib.SetMode(this, (int) displayWidth, (int) displayHeight, BackBufferFormats.BufferRGB888, !fullscreen,
//                           false, false, refresh);
//            CluwneLib.Screen.BackgroundColor = Color.FromArgb(50, 50, 50);
//            CluwneLib.CurrentClippingViewport = new Viewport(0, 0, CluwneLib.Screen.Width, CluwneLib.Screen.Height);
//            CluwneLib.DeviceReset += MainWindowResizeEnd;
//            //Gorgon.MinimumFrameTime = PreciseTimer.FpsToMilliseconds(66);
//            CluwneLib.Idle += GorgonIdle;
//        }

        private void SetupCluwne()
        {
            uint displayWidth = _configurationManager.GetDisplayWidth();
            uint displayHeight = _configurationManager.GetDisplayHeight();
            bool fullscreen = _configurationManager.GetFullscreen();
            var refresh = (int) _configurationManager.GetDisplayRefresh();

          
             

            CluwneLib.Initialize();
            CluwneLib.Go();
           
            CluwneLib.SetMode((int) displayWidth, (int) displayHeight, fullscreen,false,false,refresh);
            CluwneLib.Screen.BackgroundColor = CluwneLib.SystemColorToSFML(Color.Black);
           

            CluwneLib.Idle += CluwneLibIdle;
        }

        private void SetupInput()
        {

            //Cursor.Hide();
            CluwneLib.Screen.Resized += MainWindowResizeEnd;

            CluwneLib.Screen.KeyPressed  += KeyDownEvent;
            CluwneLib.Screen.KeyReleased += KeyUpEvent;

            CluwneLib.Screen.MouseButtonPressed  += MouseDownEvent;
            CluwneLib.Screen.MouseButtonReleased += MouseUpEvent;
            CluwneLib.Screen.MouseMoved          += MouseMoveEvent;
            CluwneLib.Screen.MouseWheelMoved     += MouseWheelMoveEvent;

            IoCManager.Resolve<IKeyBindingManager>().Initialize();
        }

        #endregion

        #endregion
    }
}
    