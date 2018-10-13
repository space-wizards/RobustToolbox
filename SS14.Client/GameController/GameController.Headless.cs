using System;
using System.Collections.Generic;

#if NOGODOT
namespace SS14.Client
{
    public partial class GameController
    {
        public static void Main()
        {
#if !X64
            throw new InvalidOperationException("The client cannot start outside x64.");
#endif

            var gc = new GameController();
            gc.Startup();
        }

        public ICollection<string> GetCommandLineArgs()
        {
            return Environment.GetCommandLineArgs();
        }
    }
}
#endif
