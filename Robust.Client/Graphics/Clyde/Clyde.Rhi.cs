using System;
using Robust.Client.Graphics.Rhi;
using Robust.Client.Graphics.Rhi.WebGpu;
using Robust.Shared;
using Robust.Shared.Utility;
using RhiBase = Robust.Client.Graphics.Rhi.RhiBase;

namespace Robust.Client.Graphics.Clyde;

internal sealed partial class Clyde
{
    public RhiBase Rhi { get; private set; } = default!;

    private void InitRhi()
    {
        DebugTools.Assert(_windowing != null);
        DebugTools.Assert(_mainWindow != null);

        var graphicsApi = _cfg.GetCVar(CVars.DisplayRhi);
        _logManager.GetSawmill("clyde.rhi").Debug("Initializing RHI {RhiName}", graphicsApi);

        Rhi = graphicsApi switch
        {
            "webGpu" => new RhiWebGpu(_deps),
            _ => throw new Exception($"Unknown RHI: {graphicsApi}")
        };

        Rhi.Init(new RhiBase.RhiInitParams
        {
            Backends = _cfg.GetCVar(CVars.DisplayWgpuBackends),
            PowerPreference = (RhiPowerPreference)_cfg.GetCVar(CVars.DisplayGpuPowerPreference)
        });
    }

    private void AcquireSwapchainTextures()
    {
        // wgpu doesn't like it if we don't do anything with the swap chain images we acquire.
        // To be safe, let's just always clear them every frame.

        var encoder = Rhi.CreateCommandEncoder(new RhiCommandEncoderDescriptor("Clear acquired swap chains"));

        foreach (var window in _windows)
        {
            window.CurSwapchainView?.Dispose();
            window.CurSwapchainView = Rhi.CreateTextureViewForWindow(window);

            var pass = encoder.BeginRenderPass(new RhiRenderPassDescriptor(
                new[]
                {
                    new RhiRenderPassColorAttachment(window.CurSwapchainView, RhiLoadOp.Clear, RhiStoreOp.Store)
                }
            ));

            pass.End();
        }

        Rhi.Queue.Submit(encoder.Finish());
    }

    private void PresentWindows()
    {
        foreach (var window in _windows)
        {
            if (window.CurSwapchainView == null)
                return;

            window.CurSwapchainView.Dispose();
            window.CurSwapchainView = null;

            Rhi.WindowPresent(window);
        }
    }
}
