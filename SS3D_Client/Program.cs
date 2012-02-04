using System;
using System.Linq;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;

using SS13.Modules;
using SS13.States;
using SS13.Modules.Network;
using SS13.UserInterface;

using Lidgren;
using Lidgren.Network;

using CSScriptLibrary;
using csscript;

using CGO;
using System.CodeDom.Compiler;
using System.IO;
using System.CodeDom;

using ClientConfigManager;
using ClientServices;

namespace SS13
{
  public class Program
  {
    private StateManager stateMgr;
    public StateManager mStateMgr
    { 
        get { return stateMgr; }
        private set { stateMgr = value; }
    }

    private NetworkManager networkMgr;
    public NetworkManager mNetworkMgr
    {
        get { return networkMgr; }
        private set { networkMgr = value; }
    }

    private NetworkGrapher netGrapher;
    public NetworkGrapher NetGrapher
    {
        get { return netGrapher; }
        set { netGrapher = value; }
    }

    private MainWindow gorgonForm;
    public MainWindow GorgonForm
    {
        get { return gorgonForm; }
        private set { gorgonForm = value; }
    }

    /************************************************************************/
    /* program starts here                                                  */
    /************************************************************************/
    [STAThread]
    static void Main(string[] args)
    {
        // Create main program
        Program prg = new Program();

        // Load Config.
        ConfigManager.Singleton.Initialize("./config.xml");
        
        // Create state manager
        prg.mStateMgr = new StateManager(prg);

        // Create main form
        prg.GorgonForm = new MainWindow(prg);

        // Create Network Manager
        prg.mNetworkMgr = new NetworkManager(prg);

        //Create Network Grapher
        prg.NetGrapher = new NetworkGrapher(prg.mNetworkMgr);

        //Initialize Services
        ServiceManager.Singleton.AddService(new CollisionManager());
        ServiceManager.Singleton.AddService(ClientServices.Lighting.LightManager.Singleton);

        Gorgon.Idle += new FrameEventHandler(prg.GorgonIdle);
        System.Windows.Forms.Application.Run(prg.GorgonForm);
    }

    public void GorgonIdle(object sender, FrameEventArgs e)
    {

    }
      
    public Program()
    {
        //Constructor
    }

  }

}
