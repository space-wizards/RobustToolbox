using System;
using ClientServices.Resources;
using SS13.Modules;
using SS13.Modules.Network;
using ClientServices;
using ClientServices.Configuration;
using ClientServices.Lighting;
using ClientServices.Collision;
using SS13.UserInterface;

namespace SS13
{
  public class Program
  {
      public StateManager StateManager { get; private set; }

      public NetworkManager NetworkManager { get; private set; }

      public NetworkGrapher NetGrapher { get; set; }

      public MainWindow GorgonForm { get; private set; }

      /************************************************************************/
    /* program starts here                                                  */
    /************************************************************************/
    [STAThread]
    static void Main(string[] args)
    {
        // Create main program
        var program = new Program();

        // Load Config.
        ServiceManager.Singleton.Register<ConfigurationManager>();
        ServiceManager.Singleton.GetService<ConfigurationManager>().Initialize("./config.xml");
        
        // Create state manager
        program.StateManager = new StateManager(program);

        // Create main form
        program.GorgonForm = new MainWindow(program);

        // Create Network Manager
        program.NetworkManager = new NetworkManager(program);

        //Create Network Grapher
        program.NetGrapher = new NetworkGrapher(program.NetworkManager);

        //Initialize Services
        ServiceManager.Singleton.Register<CollisionManager>();
        ServiceManager.Singleton.Register<LightManager>();
        ServiceManager.Singleton.Register<UiManager>();

        System.Windows.Forms.Application.Run(program.GorgonForm);
    }
  }

}
