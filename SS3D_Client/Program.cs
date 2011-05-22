using System;
using System.Linq;

using MOIS;
using Mogre;

using Miyagi;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Plugin.Input.Mois;

using SS3D.Modules;
using SS3D.States;
using SS3D.Modules.Network;
using SS3D.Modules.UI;

using Lidgren;
using Lidgren.Network;

namespace SS3D
{
  public class Program
  {
    private static OgreManager mEngine;
    private static StateManager mStateMgr;
    private static MOIS.InputManager mInputMgr;

    private bool autoConnect = false;
    
    private MiyagiSystem mMiyagiSystem;

    private NetworkManager mNetworkMgr;

    private MOIS.Keyboard mKeyboard;
    private MOIS.Mouse mMouse;
    //Create network manager.

    /************************************************************************/
    /* program starts here                                                  */
    /************************************************************************/
    [STAThread]
    static void Main(string[] args)
    {
      // create Ogre manager
      mEngine = new OgreManager();

      // create state manager
      mStateMgr = mEngine.mStateMgr = new StateManager( mEngine );
      
      // create main program
      Program prg = new Program();

      // parse command line arguments
      prg.ParseCommandLine(args);

      //Create Miyagi
      prg.mMiyagiSystem = new MiyagiSystem("Mogre");
      mEngine.mMiyagiSystem = prg.mMiyagiSystem;

      //Create & Init Miyagi Resource Manager - Must happen BEFORE resources are loaded.
      MiyagiResources.Singleton.Initialize(prg.mMiyagiSystem);

      //Load Config.
      ConfigManager.Singleton.Initialize("./config.xml");
      
      //Create Network Manager
      prg.mNetworkMgr = new NetworkManager(mEngine, mStateMgr);
      mEngine.mNetworkMgr = prg.mNetworkMgr;

      //try to initialize Ogre
      if (mEngine.Startup(ConfigManager.Singleton.Configuration))
      {
          //Load Resources. If this doesnt work because some user made a mistake
          //while editing the config - the program will crash.
          ConfigManager.Singleton.LoadResources();

          //Create Console
          GameConsole.Singleton.Initialize(mEngine);

          //Create input handlers
          int windowHandle;
          mEngine.Window.GetCustomAttribute("WINDOW", out windowHandle);

          MOIS.ParamList pl = new ParamList();
          pl.Insert("WINDOW", windowHandle.ToString());

          //Stuff to make the mouse able to exit the window
          pl.Insert("w32_mouse", "DISCL_FOREGROUND" );
          pl.Insert("w32_mouse", "DISCL_NONEXCLUSIVE");
          pl.Insert("w32_keyboard", "DISCL_FOREGROUND");
          pl.Insert("w32_keyboard", "DISCL_NONEXCLUSIVE");
          mInputMgr = MOIS.InputManager.CreateInputSystem(pl);

          prg.mKeyboard = (MOIS.Keyboard)mInputMgr.CreateInputObject(MOIS.Type.OISKeyboard, true);
          prg.mMouse = (MOIS.Mouse)mInputMgr.CreateInputObject(MOIS.Type.OISMouse, true);

          prg.mMiyagiSystem.PluginManager.LoadPlugin("Miyagi.Plugin.Input.Mois.dll", prg.mKeyboard, prg.mMouse);

          LogManager.Singleton.LogMessage("Managers loaded successfully.");

          //Set Mouse Area
          MOIS.MouseState_NativePtr state = prg.mMouse.MouseState;
          state.width = (int)mEngine.Window.Width;
          state.height = (int)mEngine.Window.Height;

          //Setup input events.
          mEngine.Root.FrameStarted += new FrameListener.FrameStartedHandler(prg.FrameStarted);
          prg.mKeyboard.KeyPressed += new KeyListener.KeyPressedHandler(prg.ProcessKeyPress);
          prg.mKeyboard.KeyReleased += new KeyListener.KeyReleasedHandler(prg.ProcessKeyRelease);
          prg.mMouse.MousePressed += new MouseListener.MousePressedHandler(prg.ProcessMousePress);
          prg.mMouse.MouseReleased += new MouseListener.MouseReleasedHandler(prg.ProcessMouseRelease);
          prg.mMouse.MouseMoved += new MouseListener.MouseMovedHandler(prg.ProcessMouseMove);

          mEngine.Window.SetDeactivateOnFocusChange(false);
          //Try to initialize the state manager.
          if (mStateMgr.Startup(typeof(MainMenu))) 
          {
              // autoconnect to default host (127.0.0.1) if command line switch set
              if (prg.autoConnect)
              {
                  if (mStateMgr.RequestStateChange(typeof(ConnectMenu)))
                  {
                      prg.UpdateScene();
                      ((ConnectMenu)mStateMgr.mCurrentState).StartConnect();
                  }
              }

              //Run main loop until the window is closed
              while (!mEngine.Window.IsClosed && Environment.HasShutdownStarted == false)
              {
                  // update networking
                  mEngine.mNetworkMgr.UpdateNetwork();

                  // update the objects in the scene
                  prg.UpdateScene();

                  // update Ogre and render the current frame
                  mEngine.Update();
              }
          }
      }
      
      // shutdown networking.
      mEngine.mNetworkMgr.ShutDown();

      // shut down state manager
      mStateMgr.Shutdown();

      // shutdown interface
      prg.mMiyagiSystem.Dispose();
      prg.mMiyagiSystem = null;

      // shut down Ogre
      mEngine.Shutdown();
    }

    #region Update, Create and Remove scene , Constructor, Command line
    public Program()
    {
        //Constructor
    }

    public void UpdateScene()
    {
        //update the state manager - this will update the active state.
        mStateMgr.Update(0);
    }

    // parse command line
    // for now, only care about -auto to autoconnect to local host  
    // make more general if other switches are required
    void ParseCommandLine(string[] args)
    {
        if (args.Length > 0 && args.Contains("-auto"))
            autoConnect = true;
    }

    bool FrameStarted(FrameEvent evt)
    {
        mKeyboard.Capture();
        mMouse.Capture();

        ProcessUnbufferedInput(evt);

        if (mMiyagiSystem != null)
            mEngine.mMiyagiSystem.Update();

        return true;
    }

    #endregion

    #region Input

    bool ProcessUnbufferedInput(FrameEvent evt)
    {
        mStateMgr.UpdateInput(evt, mKeyboard, mMouse);
        return true;
    }

    bool ProcessKeyPress(KeyEvent evt)
    {
        if (evt.key == MOIS.KeyCode.KC_ESCAPE)
            mEngine.Window.Destroy();

        if (evt.key == MOIS.KeyCode.KC_F12)
            GameConsole.Singleton.Visible = !GameConsole.Singleton.Visible;

        mStateMgr.KeyDown(evt);
        return true;
    }

    bool ProcessKeyRelease(KeyEvent evt)
    {
        mStateMgr.KeyUp(evt);
        return true;
    }

    bool ProcessMousePress(MouseEvent evt, MouseButtonID button)
    {
        mStateMgr.MouseDown(evt,button);
        return true;
    }

    bool ProcessMouseRelease(MouseEvent evt, MouseButtonID button)
    {
        mStateMgr.MouseUp(evt, button);
        return true;
    }

    bool ProcessMouseMove(MouseEvent evt)
    {
        mStateMgr.MouseMove(evt);
        return true;
    }

    #endregion

      
  }

}
