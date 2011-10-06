using System;
using System.Linq;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;

using SS3D.Modules;
using SS3D.States;
using SS3D.Modules.Network;
using SS3D.Modules.UI;

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

namespace SS3D
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

        //Load Moar types
        prg.loadTypes();

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

        Gorgon.Idle += new FrameEventHandler(prg.GorgonIdle);
        System.Windows.Forms.Application.Run(prg.GorgonForm);
    }

    public void GorgonIdle(object sender, FrameEventArgs e)
    {

    }

    public void loadTypes()
    {
        /*var asm = CSScript.Load("hat.cs");
        Module[] modules = asm.GetModules();
        Type blah = null;
        foreach (Module module in modules)
        {
            if(module.GetType("SS3D.Atom.Item.Tool.Hat") != null)
                blah = module.GetType("SS3D.Atom.Item.Tool.Hat");
        }

        Atom.Atom atom;

        if(blah != null)
            atom = (Atom.Atom)Activator.CreateInstance(blah);
        //Type atomType = asm.GetModules().GetType("SS3D.Atom.Item.Tool.Hat");
        //object atom = Activator.CreateInstance(atomType); // Create atom of type atomType with parameters uid, this
        //AsmHelper script = new AsmHelper(Asm);
        //var poo = script.CreateObject("SS3D.Atom.Item.Tool.Hat");
        */
    }

    public Program()
    {
        //Constructor
    }

  }

}
