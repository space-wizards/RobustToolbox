using System.Globalization;
using System.Reflection;
using System.Text;
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

namespace Robust.LanguageServer;

class Program
{
    static void Main(string[] args)
    {
        var deps = IoCManager.InitThread();
        ServerIoC.RegisterIoC(deps);
        deps.BuildGraph();
        SetupLogging(deps);
        InitReflectionManager(deps);

        // Console.WriteLine(PathHelpers.ExecutableRelativeFile("data"));
        Console.WriteLine($"c: {CVars.AuthMode}");

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        var _resources = deps.Resolve<IResourceManagerInternal>();
        var _config = deps.Resolve<INetConfigurationManagerInternal>();
        var _serialization = deps.Resolve<ISerializationManager>();

        // _config.LoadCVarsFromAssembly(typeof(AudioSystem).Assembly); // Robust.Server
        _config.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Robust.Shared

        // CommandLineArgs? _commandLineArgs = null;


        // var dataDir = Options.LoadConfigAndUserData
        //     ? _commandLineArgs?.DataDir ?? PathHelpers.ExecutableRelativeFile("data")
        //     : null;
        string? dataDir = null;

        // Set up the VFS
        _resources.Initialize(dataDir);

        ServerOptions Options = new();

        var mountOptions = Options.MountOptions;
        // var mountOptions = _commandLineArgs != null
        //     ? MountOptions.Merge(_commandLineArgs.MountOptions, Options.MountOptions) : Options.MountOptions;

        ProgramShared.DoMounts(_resources,
            mountOptions,
            Options.ContentBuildDirectory,
            Options.AssemblyDirectory,
            Options.LoadContentResources,
            Options.ResourceMountDisabled,
            false);


        // resourceMan.MountContentDirectory(@"/Users/ciaran/code/ss14/space-station-14/Resources");


        var _modLoader = IoCManager.Resolve<IModLoaderInternal>();
        // _modLoader.SetUseLoadContext(!ContentStart);

        // _logger =

        var resourceManifest = ResourceManifestData.LoadResourceManifest(_resources);

        Console.WriteLine(
            $"Options.AssemblyDirectory: {Options.AssemblyDirectory} - {resourceManifest.AssemblyPrefix} - {Options.ContentModulePrefix}");

        if (!_modLoader.TryLoadModulesFrom(Options.AssemblyDirectory,
                resourceManifest.AssemblyPrefix ?? Options.ContentModulePrefix))
        {
            Console.Error.WriteLine("Errors while loading content assemblies.");
            return;
        }

        foreach (var loadedModule in _modLoader.LoadedModules)
        {
            // Console.WriteLine($"Loaded: {loadedModule}");
            _config.LoadCVarsFromAssembly(loadedModule);
        }

        Console.WriteLine(typeof(Robust.Server.GameObjects.PointLightComponent));
        //
        // var entMan = deps.Resolve<IEntityManager>();
        // entMan.Initialize();

        var componentFactory = deps.Resolve<IComponentFactory>();
        componentFactory.DoAutoRegistrations();
        componentFactory.RegisterTypes([typeof(Robust.Server.GameObjects.PointLightComponent)]);

        componentFactory.IgnoreMissingComponents("Visuals");
        // componentFactory.IgnoreMissingComponents();

        AddComponentIgnores(componentFactory);

        componentFactory.GenerateNetIds();

        var comp = componentFactory.GetComponent("PointLight");
        // Console.WriteLine($"Comp: {comp}");
        // return;

        // componentFactory.RegisterIgnore(IgnoredComponents.List);

        // foreach (var c in componentFactory.AllRegisteredTypes)
        // {
        //     Console.WriteLine($"Registered component: {c.FullName}");
        // }


        var loc = IoCManager.Resolve<ILocalizationManager>();
        var culture = new CultureInfo("en-US", false);
        loc.LoadCulture(culture);

        _serialization.Initialize();

        // protoMan.ClearIgnored();

        // Dictionary<Type, HashSet<string>> modified = new();
        protoMan.Initialize();
        // protoMan.ReloadPrototypeKinds();

        protoMan.RegisterIgnore("parallax");

        // foreach (var kind in protoMan.GetPrototypeKinds())
        // {
        //     Console.Error.WriteLine($"Kind: {kind}");
        // }

        Dictionary<Type, HashSet<string>> changed = new();
        protoMan.LoadDirectory(new(@"/EnginePrototypes"), false, changed);
        protoMan.LoadDirectory(new(@"/Prototypes"), false, changed);
        protoMan.ResolveResults();

        Console.WriteLine($"protoMan: {protoMan} - changed = {changed.Count}");

        // foreach (var (type, list) in changed)
        // {
        //     Console.WriteLine($"New {type}");
        //     foreach (var x in list)
        //         Console.WriteLine($"* {x} - {protoMan.Index(type, x)}");
        // }

        // var server = deps.Resolve<IBaseServerInternal>();

        var reagentProto = protoMan.GetKindType("flavor");
        Console.WriteLine($"reagentProto: {reagentProto}");
        Console.WriteLine($"reagentProto: {protoMan.Index(reagentProto, "savory")}");
        //
        // foreach (var p in protoMan.EnumeratePrototypes(reagentProto))
        // {
        //     Console.WriteLine($"A reagent: {p}");
        // }

        return;

        // var allErrors = protoMan.ValidateDirectory(new(@"/Prototypes"));
        string filePath = "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Reagents/medicine.yml";
        // string filePath = "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Flavors/flavors.yml";
        using TextReader reader = new StreamReader(filePath);
        var allErrors = protoMan.ValidateSingleFile(reader, out _, filePath);
        // var allErrors = protoMan.ValidateDirectory(new(@"/Prototypes/Reagents"), out _);
        // foreach (var (file, errors) in allErrors)
        // {
        //     Console.WriteLine($"File: {file} - {errors.Count} errors");
        // }

        foreach (var (path, nodeList) in allErrors)
        {
            Console.Error.WriteLine($"Error in file: {path}");

            foreach (var node in nodeList)
            {
                Console.Error.WriteLine(
                    $"* {node.Node} - {node.ErrorReason} - {node.AlwaysRelevant} - {node.Node.Start} -> {node.Node.End}");
            }
        }
    }

    internal static void InitReflectionManager(IDependencyCollection deps)
    {
        // gets a handle to the shared and the current (server) dll.
        deps.Resolve<IReflectionManager>()
            .LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
    }

    internal static void SetupLogging(IDependencyCollection deps)
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
                System.Console.WriteLine($"UnhandledException but sawmill is disposed! {message}");
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
                System.Console.WriteLine($"UnobservedTaskException but sawmill is disposed! {args.Exception}");
            }
#if EXCEPTION_TOLERANCE
                args.SetObserved(); // don't crash
#endif
        };
    }

    // This list is copied from Content.Server.Entry.IgnoredComponents
    // Would be preferable to move this to a data file if needed
    private static void AddComponentIgnores(IComponentFactory factory)
    {
        var list = new[] {
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
}
