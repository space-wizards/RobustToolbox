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

        var graphicsApi = _cfg.GetCVar(CVars.DisplayRhi);
        _logManager.GetSawmill("clyde.rhi").Debug("Initializing RHI {RhiName}", graphicsApi);

        Rhi = graphicsApi switch
        {
            "webGpu" => new RhiWebGpu(this, _deps),
            _ => throw new Exception($"Unknown RHI: {graphicsApi}")
        };

        Rhi.Init();
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
