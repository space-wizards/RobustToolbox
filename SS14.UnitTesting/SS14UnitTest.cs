using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;


namespace SS14.UnitTesting
{

  
    public class SS14UnitTest
    {

        private Clock clock;
        private FrameEventArgs frameEvent;
        public event EventHandler InjectedMethods;

        public SS14UnitTest()
        {
            /* 
             * Assembly.getEntryAssembly() returns null because Unit tests 
             * are unmanaged and have no app domain managers.
             * this causes IOCManager to never load or build any of the types 
             * 
             * Fixed by Setting the Entry assembly values manually here
             */
            Assembly assembly = Assembly.GetCallingAssembly();

            AppDomainManager manager = new AppDomainManager();
            FieldInfo entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
            entryAssemblyfield.SetValue(manager, assembly);

            AppDomain domain = AppDomain.CurrentDomain;
            FieldInfo domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
            domainManagerField.SetValue(domain, manager);

            /* end fix */
            
           
            //ConfigurationManager setup
            GetConfigurationManager = IoCManager.Resolve<IPlayerConfigurationManager>();
            GetConfigurationManager.Initialize("./player_config.xml");

            //ResourceManager Setup
            GetResourceManager = IoCManager.Resolve<IResourceManager>();
            InitializeResources();

        }

        #region Setup 
        public void InitializeResources()
        {
            GetResourceManager.LoadBaseResources();
            GetResourceManager.LoadLocalResources();

        }

        public void InitializeCluwneLib()
        {           

            CluwneLib.Initialize();
            CluwneLib.Screen.BackgroundColor = Color.Black;          
            CluwneLib.Screen.Closed += MainWindowRequestClose;
         
            CluwneLib.Go();         

        }


        public void StartCluwneLibLoop()
        {
            while (CluwneLib.IsRunning)
           {
               var lastFrameTime = clock.ElapsedTime.AsSeconds();
               clock.Restart();
               frameEvent = new FrameEventArgs(lastFrameTime);
               CluwneLib.ClearCurrentRendertarget(Color.Black);
               CluwneLib.Screen.DispatchEvents();
               InjectedMethods.Invoke(this, frameEvent);
               CluwneLib.Screen.Display();
           }
        }

        private void MainWindowRequestClose(object sender, EventArgs e)
        {
            CluwneLib.Stop();
            Application.Exit();
        }    

        #endregion

        #region Accessors

        public IPlayerConfigurationManager GetConfigurationManager
        {
            get;
            private set;
        }

        public IResourceManager GetResourceManager
        {
            get;
            private set;
        }


        #endregion

    }
}
