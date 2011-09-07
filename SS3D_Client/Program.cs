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
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;
using System.Runtime.Remoting;

namespace SS3D
{
    public class Program : MarshalByRefObject, IProgram
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
            Evidence e = new Evidence(AppDomain.CurrentDomain.Evidence); // Some evidence bullshit we need

            AppDomainSetup info = new AppDomainSetup(); // Appdomain parameters
            info.ApplicationBase = Assembly.GetExecutingAssembly().CodeBase;

            /// SET UP PERMISSIONS
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            //permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

            /// Create app domain
            AppDomain mainDomain = AppDomain.CreateDomain("mainDomain", e, info, permissions);

            /// Load SS3D_Server into the new app domain and instantiate EntryPoint
            ObjectHandle handle = Activator.CreateInstanceFrom(mainDomain, typeof(Program).Assembly.ManifestModule.FullyQualifiedName, typeof(Program).FullName);
            /// Align to interface so we can use args
            IProgram prg = (IProgram)handle.Unwrap();

            ///Start the server with args.
            prg.StartProgram(args);
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

        public void StartProgram(string[] args)
        {
            // Load Config.
            ConfigManager.Singleton.Initialize("./config.xml");

            //Load Moar types
            loadTypes();

            // Create state manager
            mStateMgr = new StateManager(this);

            // Create main form
            GorgonForm = new MainWindow(this);

            // Create Network Manager
            mNetworkMgr = new NetworkManager(this);

            Gorgon.Idle += new FrameEventHandler(GorgonIdle);
            System.Windows.Forms.Application.Run(GorgonForm);
        }

    }

    public interface IProgram
    {
        void StartProgram(string[] args);
    }

}
