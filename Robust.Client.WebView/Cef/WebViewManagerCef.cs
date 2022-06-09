using System;
using System.IO;
using System.Net;
using System.Reflection;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef
{
    internal sealed partial class WebViewManagerCef : IWebViewManagerImpl
    {
        private static readonly string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!;

        private CefApp _app = default!;

        [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceManagerInternal _resourceManager = default!;

        public void Initialize()
        {
            IoCManager.Instance!.InjectDependencies(this, oneOff: true);

            string subProcessName;
            if (OperatingSystem.IsWindows())
                subProcessName = "Robust.Client.WebView.exe";
            else if (OperatingSystem.IsLinux())
                subProcessName = "Robust.Client.WebView";
            else
                throw new NotSupportedException("Unsupported platform for CEF!");

            var subProcessPath = Path.Combine(BasePath, subProcessName);
            var cefResourcesPath = LocateCefResources();

            // System.Console.WriteLine(AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES"));

            if (cefResourcesPath == null)
                throw new InvalidOperationException("Unable to locate cef_resources directory!");

            var cachePath = "";
            if (_resourceManager.UserData is WritableDirProvider userData)
                cachePath = userData.GetFullPath(new ResourcePath("/cef_cache"));

            var settings = new CefSettings()
            {
                WindowlessRenderingEnabled = true, // So we can render to our UI controls.
                ExternalMessagePump = false, // Unsure, honestly. TODO CEF: Research this?
                NoSandbox = true, // Not disabling the sandbox crashes CEF.
                BrowserSubprocessPath = subProcessPath,
                LocalesDirPath = Path.Combine(cefResourcesPath, "locales"),
                ResourcesDirPath = cefResourcesPath,
                RemoteDebuggingPort = 9222,
                CookieableSchemesList = "usr,res",
                CachePath = cachePath,
            };

            Logger.Info($"CEF Version: {CefRuntime.ChromeVersion}");

            _app = new RobustCefApp();

            // We pass no main arguments...
            CefRuntime.Initialize(new CefMainArgs(null), settings, _app, IntPtr.Zero);

            // TODO CEF: After this point, debugging breaks. No, literally. My client crashes but ONLY with the debugger.
            // I have tried using the DEBUG and RELEASE versions of libcef.so, stripped or non-stripped...
            // And nothing seemed to work. Odd.

            CefRuntime.RegisterSchemeHandlerFactory("res", "",
                new ResourceSchemeFactoryHandler(_resourceManager,
                    _dependencyCollection.Resolve<ILogManager>().RootSawmill));
        }

        private sealed class ResourceSchemeFactoryHandler : CefSchemeHandlerFactory
        {
            private readonly IResourceManager _resourceManager;
            private readonly ISawmill _sawmill;

            public ResourceSchemeFactoryHandler(IResourceManager resourceManager, ISawmill sawmill)
            {
                _resourceManager = resourceManager;
                _sawmill = sawmill;
            }

            protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName,
                CefRequest request)
            {
                var uri = new Uri(request.Url);

                _sawmill.Debug($"HANDLING: {request.Url}");

                if (_resourceManager.TryContentFileRead(uri.AbsolutePath, out var stream))
                {
                    var mime = "text/plain";
                    if (uri.AbsolutePath.EndsWith(".png"))
                        mime = "image/png";
                    else if (uri.AbsolutePath.EndsWith(".html"))
                        mime = "text/html";

                    return new RequestResultStream(stream, mime, HttpStatusCode.OK).MakeHandler();
                }
                else
                {
                    return new RequestResultStream(_resourceManager.ContentFileRead("/404.txt"), "text/plain",
                        HttpStatusCode.NotFound).MakeHandler();
                    // obj.DoRespondStream(_res.ContentFileRead("/404.txt"), "text/plain");
                }
            }
        }

        private static string? LocateCefResources()
        {
            if (ProbeDir(BasePath, out var path))
                return path;


            foreach (var searchDir in NativeDllSearchDirectories())
            {
                if (ProbeDir(searchDir, out path))
                    return path;
            }

            return null;

            static bool ProbeDir(string dir, out string path)
            {
                path = Path.Combine(dir, "cef_resources");
                return Directory.Exists(path);
            }
        }

        internal static string[] NativeDllSearchDirectories()
        {
            var sepChar = OperatingSystem.IsWindows() ? ';' : ':';

            var searchDirectories = ((string)AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES")!)
                .Split(sepChar, StringSplitOptions.RemoveEmptyEntries);

            return searchDirectories;
        }

        public void Update()
        {
            // Calling this makes CEF do its work, without using its own update loop.
            CefRuntime.DoMessageLoopWork();
        }

        public void Shutdown()
        {
            CefRuntime.Shutdown();
        }
    }
}
