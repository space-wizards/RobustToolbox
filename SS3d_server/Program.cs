using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_Server.Modules;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;

namespace SS3D_Server
{
    class EntryPoint : MarshalByRefObject, IEntryPoint
    {
        private SS3DServer server;

        static void Main(string[] args)
        {
            Evidence e = new Evidence(AppDomain.CurrentDomain.Evidence); // Some evidence bullshit we need

            AppDomainSetup info = new AppDomainSetup(); // Appdomain parameters
            info.ApplicationBase = Assembly.GetExecutingAssembly().CodeBase;

            /// SET UP PERMISSIONS
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            //permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

            /// Create app domain
            AppDomain mainDomain = AppDomain.CreateDomain("mainDomain", e, info, permissions);

            /// Load SS3D_Server into the new app domain and instantiate EntryPoint
            ObjectHandle handle = Activator.CreateInstanceFrom(mainDomain, typeof(EntryPoint).Assembly.ManifestModule.FullyQualifiedName, typeof(EntryPoint).FullName);
            /// Align to interface so we can use args
            IEntryPoint entryPoint = (IEntryPoint)handle.Unwrap();

            ///Start the server with args.
            entryPoint.StartServer(args);
        }

        public EntryPoint()
        {
            server = new SS3DServer();
        }

        public void StartServer(string[] args)
        {
      
            //EntryPoint main = new EntryPoint();
            LogManager.Log("Server -> Starting");

            if (server.Start())
            {
                LogManager.Log("Server -> Can not start server", LogLevel.Fatal); //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogManager.Log("Server Version " + strVersion + " -> Ready");

            server.MainLoop();
        }


    }

    public interface IEntryPoint
    {
        void StartServer(string[] args);
    }
}
