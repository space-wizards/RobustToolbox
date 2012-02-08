using System;
using ClientInterfaces;
using SS13.IoC;

namespace SS13
{
  public class Program
  {
    /************************************************************************/
    /* program starts here                                                  */
    /************************************************************************/
    [STAThread]
    static void Main(string[] args)
    {
        // Load Config.
        IoCManager.Resolve<IConfigurationManager>().Initialize("./config.xml");
        
        System.Windows.Forms.Application.Run(new MainWindow());
    }
  }

}
