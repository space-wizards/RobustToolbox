using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Robust.Client.Console;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ILocalizationManager _localization = default!;

        private const int BasePort = 9222;
        private ISawmill _sawmill = default!;
        private const string BaseCacheName = "cef_cache";
        private const string LockFileName = "robust.lock";
        private FileStream? _lockFileStream = null;

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("web.cef");

            _consoleHost.RegisterCommand(
                "flushcookies",
                _localization.GetString("cmd-flushcookies-desc"),
                _localization.GetString("cmd-flushcookies-help"),
                (_, _, _) => CefCookieManager.GetGlobal(null).FlushStore(null));

            string subProcessName;
            if (OperatingSystem.IsWindows())
                subProcessName = "Robust.Client.WebView.exe";
            else if (OperatingSystem.IsLinux())
                subProcessName = "Robust.Client.WebView";
            else
                throw new NotSupportedException("Unsupported platform for CEF!");

            var subProcessPath = Path.Combine(BasePath, subProcessName);
            var cefResourcesPath = LocateCefResources();
            _sawmill.Debug($"Subprocess path: {subProcessPath}, resources: {cefResourcesPath}");

            // System.Console.WriteLine(AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES"));

            if (cefResourcesPath == null)
                throw new InvalidOperationException("Unable to locate cef_resources directory!");

            var remoteDebugPort = _cfg.GetCVar(WCVars.WebRandomDebugPort) ? new Random().Next(9221, 65535) : BasePort;

            var cachePath = "";
            if (_resourceManager.UserData is WritableDirProvider dataDir)
                cachePath = FindAndLockCacheDirectory(dataDir);

            var settings = new CefSettings()
            {
                WindowlessRenderingEnabled = true, // So we can render to our UI controls.
                ExternalMessagePump = false, // Unsure, honestly. TODO CEF: Research this?
                NoSandbox = true, // Not disabling the sandbox crashes CEF.
                BrowserSubprocessPath = subProcessPath,
                LocalesDirPath = Path.Combine(cefResourcesPath, "locales"),
                ResourcesDirPath = cefResourcesPath,
                RemoteDebuggingPort = remoteDebugPort,
                CookieableSchemesList = "usr,res",
                CachePath = cachePath,
            };

            var userAgentOverride = _cfg.GetCVar(WCVars.WebUserAgentOverride);
            if (!string.IsNullOrEmpty(userAgentOverride))
            {
                settings.UserAgent = userAgentOverride;
            }

            _sawmill.Info($"CEF Version: {CefRuntime.ChromeVersion}");

            _app = new RobustCefApp(_sawmill);

            var process = Process.GetCurrentProcess();
            Environment.SetEnvironmentVariable("ROBUST_CEF_BROWSER_PROCESS_ID", process.Id.ToString());
            Environment.SetEnvironmentVariable("ROBUST_CEF_BROWSER_PROCESS_MODULE", process.MainModule?.FileName ?? "");

            // So these arguments look like nonsense, but it turns out CEF is just *like that*.
            // The first argument is literally nonsense, but it needs to be there as otherwise the second argument doesn't apply
            // The second argument turns off CEF's bullshit error handling, which breaks dotnet's error handling.
            CefRuntime.Initialize(new CefMainArgs(["binary", "--disable-in-process-stack-traces"]),
                settings,
                _app,
                IntPtr.Zero);

            if (!_cfg.GetCVar(WCVars.WebResProtocol)) return;

            var handler = new ResourceSchemeFactoryHandler(
                this,
                _resourceManager,
                _logManager.GetSawmill("web.res"));

            CefRuntime.RegisterSchemeHandlerFactory("res", "", handler);
        }

        private string FindAndLockCacheDirectory(WritableDirProvider userData)
        {
            var finalAbsoluteCachePath = "";

            // try to find existing, unlocked cache directory
            List<string> existingCacheDirs = [];
            foreach (var entryName in userData.DirectoryEntries(new ResPath("/"))) // i love python
                if (entryName.StartsWith(BaseCacheName) && userData.IsDir(new ResPath($"/{entryName}")))
                    existingCacheDirs.Add(entryName);

            existingCacheDirs.Sort();

            foreach (var relativeDirName in existingCacheDirs)
            {
                var absoluteDirPath = userData.GetFullPath(new ResPath($"/{relativeDirName}"));
                var lockFilePath = Path.Combine(absoluteDirPath, LockFileName);

                if (!Directory.Exists(absoluteDirPath) || File.Exists(lockFilePath)) continue;
                finalAbsoluteCachePath = absoluteDirPath;
                _sawmill.Debug($"Found existing unlocked cache directory: {finalAbsoluteCachePath}");
                break;
            }

            // no suitable existing directory found? create a new one ?? IS THIS NEEDED SHOULDNT CEF make it?
            if (string.IsNullOrEmpty(finalAbsoluteCachePath))
            {
                var i = 0;
                string newRelativeCacheDir;
                do
                {
                    newRelativeCacheDir = $"{BaseCacheName}{i}";
                    finalAbsoluteCachePath = userData.GetFullPath(new ResPath($"/{newRelativeCacheDir}"));
                    i++;
                } while (userData.Exists(new ResPath($"/{newRelativeCacheDir}")));

                _sawmill.Debug($"No suitable existing cache directory. Creating new one: {finalAbsoluteCachePath}");
                try
                {
                    Directory.CreateDirectory(finalAbsoluteCachePath);
                }
                catch (IOException ex)
                {
                    _sawmill.Error($"Failed to create cache directory '{finalAbsoluteCachePath}'. Exception: {ex.Message}");
                    throw new InvalidOperationException($"Failed to create CEF cache directory: {finalAbsoluteCachePath}", ex);
                }
            }

            // lock the chosen/created directory
            var finalLockFilePath = Path.Combine(finalAbsoluteCachePath, LockFileName);
            try
            {
                if (!Directory.Exists(finalAbsoluteCachePath))
                {
                    _sawmill.Warning($"Cache directory {finalAbsoluteCachePath} was expected to exist but doesn't. Attempting to create.");
                    Directory.CreateDirectory(finalAbsoluteCachePath);
                }

                _lockFileStream = new FileStream(finalLockFilePath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    4096,
                    FileOptions.DeleteOnClose);
                _sawmill.Debug($"Successfully locked cache directory: {finalAbsoluteCachePath} with lock file: {finalLockFilePath}");
            }
            catch (IOException ex)
            {
                _sawmill.Error($"Failed to create lock file '{finalLockFilePath}' for CEF instance. Is another instance running or is there a permissions issue? Exception: {ex.Message}");
                throw new InvalidOperationException($"Failed to create lock file '{finalLockFilePath}' for CEF instance. Another instance might be using this cache path, or there could be a permissions issue.", ex);
            }

            return finalAbsoluteCachePath;
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
            _lockFileStream?.Dispose();
        }

        private sealed class ResourceSchemeFactoryHandler(
            WebViewManagerCef parent,
            IResourceManager resourceManager,
            ISawmill sawmill)
            : CefSchemeHandlerFactory
        {
            protected override CefResourceHandler Create(
                CefBrowser browser,
                CefFrame frame,
                string schemeName,
                CefRequest request)
            {
                var uri = new Uri(request.Url);

                sawmill.Debug($"HANDLING: {request.Url}");

                var resPath = new ResPath(uri.AbsolutePath);
                if (resourceManager.TryContentFileRead(resPath, out var stream))
                {
                    if (!parent.TryGetResourceMimeType(resPath.Extension, out var mime))
                        mime = "application/octet-stream";

                    return new RequestResultStream(stream, mime, HttpStatusCode.OK).MakeHandler();
                }

                var notFoundStream = new MemoryStream();
                notFoundStream.Write(Encoding.UTF8.GetBytes("Not found"));
                notFoundStream.Position = 0;

                return new RequestResultStream(notFoundStream, "text/plain", HttpStatusCode.NotFound).MakeHandler();
            }
        }
    }
}
