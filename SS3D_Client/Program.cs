using System;
using System.Linq;

using MOIS;

using GorgonLibrary;
using GorgonLibrary.Graphics;

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

      Gorgon.Idle += new FrameEventHandler(prg.GorgonIdle);
      System.Windows.Forms.Application.Run(prg.GorgonForm);
    }

    public void GorgonIdle(object sender, FrameEventArgs e)
    {
        // Update networking
        mNetworkMgr.UpdateNetwork();

        // Update the state manager - this will update the active state.
        mStateMgr.Update(e);
    }

    public Program()
    {
        //Constructor
    }

  }

}
