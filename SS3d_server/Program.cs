using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;

namespace SS3d_server
{
    class SS3D_Server
    {
        private SS3DNetserver server;

        static void Main(string[] args)
        {
            SS3D_Server main = new SS3D_Server();
            Console.WriteLine("Server -> Starting");
            main.server = new SS3DNetserver();

            if (main.server.Start())
            {
                Console.WriteLine("Server -> Can not start server"); //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }
            Console.WriteLine("Server -> Ready");

            while(main.server.Active)
            {
                main.server.ProcessPackets();
                main.server.Update();
            }
        }


    }
}
