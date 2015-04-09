
using System;

namespace SS14.Client
{
    public class Program
    {
        /************************************************************************/
        /* program starts here                                                  */
        /************************************************************************/
        private static bool fullDump = false;

        [STAThread]
        private static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            //Process command-line args
            processArgs(args);
       
            GameController GC = new GameController();
        }
        
        private static void processArgs(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--fulldump":
                        fullDump = true;
                        break;
                }
            }
        }
    }
}