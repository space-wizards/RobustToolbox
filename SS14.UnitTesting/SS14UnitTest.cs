using NUnit.Framework;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace SS14.UnitTesting
{
    public abstract class SS14UnitTest
    {
        private FrameEventArgs frameEvent;
        public delegate void EventHandler();
        public static event EventHandler InjectedMethod;

        #region Options

        // TODO: make this figured out at runtime so we don't have to pass a compiler flag.
#if HEADLESS
        public const bool Headless = true;
#else
        public const bool Headless = false;
#endif

        // These properties are meant to be overriden to disable certain parts
        // Like loading resource packs, which isn't always needed.

        /// <summary>
        /// Whether the client resource pack should be loaded or not.
        /// </summary>
        public virtual bool NeedsResourcePack => false;

        /// <summary>
        /// Whether the client config should be loaded or not.
        /// </summary>
        public virtual bool NeedsClientConfig => false;

        #endregion Options

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

#endregion Accessors

        public SS14UnitTest()
        {
            TestFixtureAttribute a = Attribute.GetCustomAttribute(GetType(), typeof(TestFixtureAttribute)) as TestFixtureAttribute;
            if (NeedsResourcePack && Headless)
            {
                // Disable the test automatically.
                a.Explicit = true;
                return;
            }

            // Clear state across tests.
            IoCManager.Clear();

            var assemblies = new List<Assembly>();
            string assemblyDir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            assemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyDir, "SS14.Client.exe")));
            assemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyDir, "SS14.Server.exe")));
            assemblies.Add(Assembly.LoadFrom(Path.Combine(assemblyDir, "SS14.Shared.dll")));
            assemblies.Add(Assembly.GetExecutingAssembly());

            IoCManager.AddAssemblies(assemblies);
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            if (NeedsClientConfig)
            {
                //ConfigurationManager setup
                GetConfigurationManager = IoCManager.Resolve<IPlayerConfigurationManager>();
                GetConfigurationManager.Initialize(
                    PathHelpers.AssemblyRelativeFile("./player_config.xml", Assembly.GetExecutingAssembly()));
            }

            if (NeedsResourcePack)
            {
                GetResourceManager = IoCManager.Resolve<IResourceManager>();
                InitializeResources();
            }
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

            CluwneLib.Video.SetWindowSize(1280, 720);
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

        #endregion Setup
    }
}
