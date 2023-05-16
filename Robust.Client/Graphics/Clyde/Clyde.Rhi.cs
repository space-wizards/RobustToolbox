using System;
using Robust.Client.Graphics.Clyde.Rhi;
using Robust.Shared;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde;

internal sealed partial class Clyde
{
    public RhiBase Rhi { get; private set; } = default!;

    private void InitRhi()
    {
        DebugTools.Assert(_windowing != null);
        DebugTools.Assert(_mainWindow != null);

        var graphicsApi = _cfg.GetCVar(CVars.DisplayGraphicsApi);
        _logManager.GetSawmill("clyde.rhi").Debug("Initializing graphics API {GraphicsApiName}", graphicsApi);

        Rhi = graphicsApi switch
        {
            "webGpu" => new RhiWebGpu(this, _deps),
            _ => throw new Exception($"Unknown graphics API: {graphicsApi}")
        };

        Rhi.Init();
    }
}
