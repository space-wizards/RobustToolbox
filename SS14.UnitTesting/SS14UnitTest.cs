using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Resource;
using SS14.Server.Interfaces;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace SS14.UnitTesting
{


    public class SS14UnitTest
    {


        private FrameEventArgs frameEvent;
        public delegate void EventHandler();
        public static event EventHandler InjectedMethod;


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

        public Clock GetClock
        {
            get;
            set;
        }


        #endregion


        public SS14UnitTest()
        {
            var assemblies = new List<Assembly>();
            string assemblyDir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            assemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyDir, "SpaceStation14.exe")));
            assemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyDir, "SpaceStation14_Server.exe")));
            assemblies.Add(Assembly.GetExecutingAssembly());

            IoCManager.AddAssemblies(assemblies);

            //ConfigurationManager setup
            GetConfigurationManager = IoCManager.Resolve<IPlayerConfigurationManager>();
            GetConfigurationManager.Initialize("./player_config.xml");

            #if !HEADLESS
            //ResourceManager Setup
            GetResourceManager = IoCManager.Resolve<IResourceManager>();
            InitializeResources();
            #endif

        }

        #region Setup
        public void InitializeResources()
        {
            GetResourceManager.LoadBaseResources();
            GetResourceManager.LoadLocalResources();

        }

        public void InitializeCluwneLib()
        {
            GetClock = new Clock();

            CluwneLib.Video.SetWindowSize(1280,720);
            CluwneLib.Video.SetFullscreen(false);
            CluwneLib.Video.SetRefreshRate(60);

            CluwneLib.Initialize();
            CluwneLib.Screen.BackgroundColor = Color.Black;
            CluwneLib.Screen.Closed += MainWindowRequestClose;

            CluwneLib.Go();
        }

        public void InitializeCluwneLib(uint width, uint height, bool fullscreen, uint refreshrate)
        {
            GetClock = new Clock();

            CluwneLib.Video.SetWindowSize(width, height);
            CluwneLib.Video.SetFullscreen(fullscreen);
            CluwneLib.Video.SetRefreshRate(refreshrate);

            CluwneLib.Initialize();
            CluwneLib.Screen.BackgroundColor = Color.Black;
            CluwneLib.Screen.Closed += MainWindowRequestClose;

            CluwneLib.Go();
        }


        public void StartCluwneLibLoop()
        {
            while (CluwneLib.IsRunning)
           {
               var lastFrameTime = GetClock.ElapsedTime.AsSeconds();
               GetClock.Restart();
               frameEvent = new FrameEventArgs(lastFrameTime);
               CluwneLib.ClearCurrentRendertarget(Color.Black);
               CluwneLib.Screen.DispatchEvents();
               InjectedMethod();
               CluwneLib.Screen.Display();
           }
        }

        private void MainWindowRequestClose(object sender, EventArgs e)
        {
            CluwneLib.Stop();
            Application.Exit();
        }

        #endregion
    }
}
