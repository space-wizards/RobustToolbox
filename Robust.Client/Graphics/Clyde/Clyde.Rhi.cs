using System;
using Robust.Client.Graphics.Rhi;
using Robust.Client.Graphics.Rhi.WebGpu;
using Robust.Client.Interop.RobustNative.Webgpu;
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
                PowerPreference = (RhiPowerPreference)_cfg.GetCVar(CVars.DisplayGpuPowerPreference),
                MainWindowSurfaceParams = _mainWindow.SurfaceParams
            },
            out _mainWindow.RhiWebGpuData);
    }

    private void AcquireSwapchainTextures()
    {
        // wgpu doesn't like it if we don't do anything with the swap chain images we acquire.
        // To be safe, let's just always clear them every frame.

        var encoder = Rhi.CreateCommandEncoder(new RhiCommandEncoderDescriptor("Clear acquired swap chains"));

        foreach (var window in _windows)
        {
            window.CurSurfaceTexture?.Dispose();
            window.CurSurfaceTextureView?.Dispose();

            if (window.NeedSurfaceReconfigure)
            {
                Rhi.WindowRecreateSwapchain(window.RhiWebGpuData!, window.FramebufferSize, VsyncEnabled);
                window.NeedSurfaceReconfigure = false;
            }

            window.CurSurfaceTexture = Rhi.GetSurfaceTextureForWindow(window.RhiWebGpuData!);
            window.CurSurfaceTextureView = window.CurSurfaceTexture.CreateView(new RhiTextureViewDescriptor
            {
                Dimension = RhiTextureViewDimension.Dim2D,
                Format = Rhi.MainTextureFormat,
                ArrayLayerCount = 1,
                MipLevelCount = 1,
                Aspect = RhiTextureAspect.All,
            });

            var pass = encoder.BeginRenderPass(new RhiRenderPassDescriptor(
                new[]
                {
                    new RhiRenderPassColorAttachment(window.CurSurfaceTextureView, RhiLoadOp.Clear, RhiStoreOp.Store)
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
            if (window.CurSurfaceTexture == null)
                return;

            window.CurSurfaceTexture.Dispose();
            window.CurSurfaceTexture = null;

            window.CurSurfaceTextureView!.Dispose();
            window.CurSurfaceTextureView = null;

            Rhi.WindowPresent(window.RhiWebGpuData!);
        }
    }
}
