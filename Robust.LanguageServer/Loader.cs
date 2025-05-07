using System.Globalization;
using System.Reflection;
using System.Text;
using Robust.Client;
using Robust.Server.GameObjects;
using Robust.Server;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Robust.LanguageServer;

public sealed class Loader
{
    [Dependency] IResourceManagerInternal _resources = default!;
    [Dependency] INetConfigurationManagerInternal _config = default!;
    [Dependency] ISerializationManager _serialization = default!;

    public void Init(IDependencyCollection deps)
    {
        SetupLogging(deps);
        // InitReflectionManager(deps);

        // Console.Error.WriteLine(PathHelpers.ExecutableRelativeFile("data"));
        Console.Error.WriteLine($"c: {CVars.AuthMode}");

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        // var _resources = deps.Resolve<IResourceManagerInternal>();
        // var _config = deps.Resolve<INetConfigurationManagerInternal>();
        // var _serialization = deps.Resolve<ISerializationManager>();

        // _config.LoadCVarsFromAssembly(typeof(AudioSystem).Assembly); // Robust.Server
        _config.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Robust.Shared

        // CommandLineArgs? _commandLineArgs = null;


        // var dataDir = Options.LoadConfigAndUserData
        //     ? _commandLineArgs?.DataDir ?? PathHelpers.ExecutableRelativeFile("data")
        //     : null;
        string? dataDir = null;

        // Set up the VFS
        _resources.Initialize(dataDir);

        var loadServer = true;

        // Why aren't these a shared interface :(
        ServerOptions serverOptions = new();
        GameControllerOptions clientOptions = new();

        if (loadServer)
        {
            ProgramShared.DoMounts(_resources,
                serverOptions.MountOptions,
                serverOptions.ContentBuildDirectory,
                serverOptions.AssemblyDirectory,
                serverOptions.LoadContentResources,
                serverOptions.ResourceMountDisabled);
        }
        else
        {
            ProgramShared.DoMounts(_resources,
                clientOptions.MountOptions,
                clientOptions.ContentBuildDirectory,
                clientOptions.AssemblyDirectory,
                clientOptions.LoadContentResources,
                clientOptions.ResourceMountDisabled);
        }

        // resourceMan.MountContentDirectory(@"/Users/ciaran/code/ss14/space-station-14/Resources");


        var _modLoader = IoCManager.Resolve<IModLoaderInternal>();
        // _modLoader.SetUseLoadContext(!ContentStart);

        // _logger =

        var resourceManifest = ResourceManifestData.LoadResourceManifest(_resources);

        Console.Error.WriteLine(
            $"Options.AssemblyDirectory: {serverOptions.AssemblyDirectory} - {resourceManifest.AssemblyPrefix} - {serverOptions.ContentModulePrefix}");

        if (!_modLoader.TryLoadModulesFrom(serverOptions.AssemblyDirectory,
                resourceManifest.AssemblyPrefix ?? serverOptions.ContentModulePrefix))
        {
            Console.Error.WriteLine("Errors while loading content assemblies.");
            return;
        }

        foreach (var loadedModule in _modLoader.LoadedModules)
        {
            // Console.Error.WriteLine($"Loaded: {loadedModule}");
            _config.LoadCVarsFromAssembly(loadedModule);
        }

        Console.Error.WriteLine(typeof(Robust.Server.GameObjects.PointLightComponent));
        //
        // var entMan = deps.Resolve<IEntityManager>();
        // entMan.Initialize();


        InitReflectionManager(deps);
        deps.Resolve<IReflectionManager>().LoadAssemblies(typeof(PointLightComponent).Assembly);
        // deps.Resolve<IReflectionManager>().LoadAssemblies(typeof(SpriteComponent).Assembly);
        // deps.Resolve<IReflectionManager>().Initialize();

        foreach (var asm in deps.Resolve<IReflectionManager>().Assemblies)
        {
            Console.Error.WriteLine("Loaded: " + asm.FullName);
        }

        var componentFactory = deps.Resolve<IComponentFactory>();
        componentFactory.DoAutoRegistrations();
        // componentFactory.RegisterTypes([typeof(Robust.Server.GameObjects.PointLightComponent)]);

        if (loadServer)
            componentFactory.IgnoreMissingComponents("Visuals");
        else
            componentFactory.IgnoreMissingComponents();
        // componentFactory.IgnoreMissingComponents();

        if (loadServer)
            AddServerComponentIgnores(componentFactory);

        componentFactory.GenerateNetIds();

        // var comp = componentFactory.GetComponent("PointLight");
        // Console.Error.WriteLine($"Comp: {comp}");
        // return;

        // componentFactory.RegisterIgnore(IgnoredComponents.List);

        // foreach (var c in componentFactory.AllRegisteredTypes)
        // {
        //     Console.Error.WriteLine($"Registered component: {c.FullName}");
        // }


        var loc = IoCManager.Resolve<ILocalizationManager>();
        var culture = new CultureInfo("en-US", false);
        loc.LoadCulture(culture);


        _serialization.Initialize();

        // protoMan.ClearIgnored();

        // Dictionary<Type, HashSet<string>> modified = new();
        protoMan.Initialize();
        // protoMan.ReloadPrototypeKinds();

        if (!loadServer)
            AddClientPrototypeIgnores(protoMan);

        protoMan.RegisterIgnore("parallax");

        // foreach (var kind in protoMan.GetPrototypeKinds())
        // {
        //     Console.Error.WriteLine($"Kind: {kind}");
        // }

        Dictionary<Type, HashSet<string>> changed = new();
        protoMan.LoadDirectory(new(@"/EnginePrototypes"), false, changed);
        Console.Error.WriteLine($"protoMan: engine {protoMan} - changed = {changed.Count}");
        protoMan.LoadDirectory(new(@"/Prototypes"), false, changed);
        protoMan.ResolveResults();

        // return;
        // deps.Resolve<IReflectionManager>().LoadAssemblies(typeof(RobustIntegrationTest).Assembly);

        Console.Error.WriteLine($"protoMan: {protoMan} - changed = {changed.Count}");

        // foreach (var (type, list) in changed)
        // {
        //     Console.Error.WriteLine($"New {type}");
        //     foreach (var x in list)
        //         Console.Error.WriteLine($"* {x} - {protoMan.Index(type, x)}");
        // }

        // var server = deps.Resolve<IBaseServerInternal>();

        var reagentProto = protoMan.GetKindType("flavor");
        Console.Error.WriteLine($"reagentProto: {reagentProto}");
        Console.Error.WriteLine($"reagentProto: {protoMan.Index(reagentProto, "savory")}");
        //
        // foreach (var p in protoMan.EnumeratePrototypes(reagentProto))
        // {
        //     Console.Error.WriteLine($"A reagent: {p}");
        // }

        // return;
    }

    private static void InitReflectionManager(IDependencyCollection deps)
    {
        // gets a handle to the shared and the current (server) dll.
        deps.Resolve<IReflectionManager>()
            .LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
    }

    private static void SetupLogging(IDependencyCollection deps)
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS_USE_UTF8_CONSOLE
                System.Console.OutputEncoding = Encoding.UTF8;
#else
            System.Console.OutputEncoding = Encoding.Unicode;
#endif
        }

        var mgr = deps.Resolve<ILogManager>();
        var handler = new ConsoleLogHandler();
        mgr.RootSawmill.AddHandler(handler);
        mgr.GetSawmill("res.typecheck").Level = LogLevel.Info;
        mgr.GetSawmill("go.sys").Level = LogLevel.Info;
        mgr.GetSawmill("loc").Level = LogLevel.Error;
        // mgr.GetSawmill("szr").Level = LogLevel.Info;

#if DEBUG_ONLY_FCE_INFO
#if DEBUG_ONLY_FCE_LOG
            var fce = mgr.GetSawmill("fce");
#endif
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                // TODO: record FCE stats
#if DEBUG_ONLY_FCE_LOG
                fce.Fatal(message);
#endif
            }
#endif

        var uh = mgr.GetSawmill("unhandled");
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var message = ((Exception)args.ExceptionObject).ToString();
            try
            {
                uh.Log(args.IsTerminating ? LogLevel.Fatal : LogLevel.Error, message);
            }
            catch (ObjectDisposedException)
            {
                // Avoid eating the exception if it's during shutdown and the sawmill is already gone.
                System.Console.Error.WriteLine($"UnhandledException but sawmill is disposed! {message}");
            }
        };

        var uo = mgr.GetSawmill("unobserved");
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            try
            {
                uo.Error(args.Exception!.ToString());
            }
            catch (ObjectDisposedException)
            {
                // Avoid eating the exception if it's during shutdown and the sawmill is already gone.
                System.Console.Error.WriteLine($"UnobservedTaskException but sawmill is disposed! {args.Exception}");
            }
#if EXCEPTION_TOLERANCE
                args.SetObserved(); // don't crash
#endif
        };
    }

    // This list is copied from Content.Server.Entry.IgnoredComponents
    // Would be preferable to move this to a data file if needed
    private static void AddServerComponentIgnores(IComponentFactory factory)
    {
        var list = new[]
        {
            "ConstructionGhost",
            "IconSmooth",
            "InteractionOutline",
            "Marker",
            "GuidebookControlsTest",
            "GuideHelp",
            "Clickable",
            "Icon",
            "CableVisualizer",
            "SolutionItemStatus",
            "UIFragment",
            "PdaBorderColor",
            "InventorySlots",
            "LightFade",
            "HolidayRsiSwap",
            "OptionsVisualizer"
        };
        factory.RegisterIgnore(list);
    }

    // Below list copied from client EntryPoint.Init
    private static void AddClientPrototypeIgnores(IPrototypeManager protoMan)
    {
        protoMan.RegisterIgnore("utilityQuery");
        protoMan.RegisterIgnore("utilityCurvePreset");
        protoMan.RegisterIgnore("accent");
        protoMan.RegisterIgnore("gasReaction");
        protoMan.RegisterIgnore("seed"); // Seeds prototypes are server-only.
        protoMan.RegisterIgnore("objective");
        protoMan.RegisterIgnore("holiday");
        protoMan.RegisterIgnore("htnCompound");
        protoMan.RegisterIgnore("htnPrimitive");
        protoMan.RegisterIgnore("gameMap");
        protoMan.RegisterIgnore("gameMapPool");
        protoMan.RegisterIgnore("lobbyBackground");
        protoMan.RegisterIgnore("gamePreset");
        protoMan.RegisterIgnore("noiseChannel");
        protoMan.RegisterIgnore("playerConnectionWhitelist");
        protoMan.RegisterIgnore("spaceBiome");
        protoMan.RegisterIgnore("worldgenConfig");
        protoMan.RegisterIgnore("gameRule");
        protoMan.RegisterIgnore("worldSpell");
        protoMan.RegisterIgnore("entitySpell");
        protoMan.RegisterIgnore("instantSpell");
        protoMan.RegisterIgnore("roundAnnouncement");
        protoMan.RegisterIgnore("wireLayout");
        protoMan.RegisterIgnore("alertLevels");
        protoMan.RegisterIgnore("nukeopsRole");
        protoMan.RegisterIgnore("ghostRoleRaffleDecider");
    }
}
