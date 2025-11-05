using System;
using System.Collections.Generic;
using Robust.Client.Graphics.FontManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics;

/// <summary>
/// Implementation of <see cref="ISystemFontManager"/> that proxies to platform-specific implementations,
/// and adds additional logging.
/// </summary>
internal sealed class SystemFontManager : ISystemFontManagerInternal, IPostInjectInit
{
    [Dependency] private readonly IFontManagerInternal _fontManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;

    private ISystemFontManagerInternal _implementation = default!;

    public bool IsSupported => _implementation.IsSupported;
    public IEnumerable<ISystemFontFace> SystemFontFaces => _implementation.SystemFontFaces;

    public void Initialize()
    {
        _implementation = GetImplementation();
        _sawmill.Verbose($"Using {_implementation.GetType()}");

        _sawmill.Debug("Initializing system font manager implementation");
        try
        {
            var sw = RStopwatch.StartNew();
            _implementation.Initialize();
            _sawmill.Debug($"Done initializing system font manager in {sw.Elapsed}");
        }
        catch (Exception e)
        {
            // This is a non-critical engine system that has to parse significant amounts of external data.
            // Best to fail gracefully to avoid full startup failures.

            _sawmill.Error($"Error while initializing system font manager, resorting to fallback: {e}");
            _implementation = new SystemFontManagerFallback();
        }
    }

    public void Shutdown()
    {
        _sawmill.Verbose("Shutting down system font manager");

        try
        {
            _implementation.Shutdown();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Exception shutting down system font manager: {e}");
            return;
        }

        _sawmill.Verbose("Successfully shut down system font manager");
    }

    private ISystemFontManagerInternal GetImplementation()
    {
        if (!_cfg.GetCVar(CVars.FontSystem))
            return new SystemFontManagerFallback();

#if WINDOWS
        return new SystemFontManagerDirectWrite(_logManager, _cfg, _fontManager);
#elif FREEDESKTOP
        return new SystemFontManagerFontconfig(_logManager, _fontManager);
#elif MACOS
        return new SystemFontManagerCoreText(_logManager, _fontManager);
#else
        return new SystemFontManagerFallback();
#endif
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("font.system");
        // _sawmill.Level = LogLevel.Verbose;
    }
}
