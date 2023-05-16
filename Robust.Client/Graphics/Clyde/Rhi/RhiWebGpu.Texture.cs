using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Utility;
using Silk.NET.WebGPU;
using WGPUTexture = Silk.NET.WebGPU.Texture;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, TextureReg> _textureRegistry = new();
    private readonly Dictionary<RhiHandle, TextureViewReg> _textureViewRegistry = new();

    public override RhiTexture CreateTexture(in RhiTextureDescriptor descriptor)
    {
        var format = descriptor.Format;
        var dimension = descriptor.Dimension;
        var usage = descriptor.Usage;
        ValidateTextureFormat(format);
        ValidateTextureDimension(dimension);
        ValidateTextureUsage(usage);

        // TODO: Copy to stackalloc instead.
        var viewFormats = descriptor.ViewFormats?.ToArray() ?? Array.Empty<RhiTextureFormat>();
        foreach (var vf in viewFormats)
        {
            ValidateTextureFormat(vf);
        }

        DebugTools.Assert(
            sizeof(RhiTextureFormat) == sizeof(TextureFormat),
            "Pointer to view formats array is cast directly to pass to native, sizes must match");

        WGPUTexture* texturePtr;
        fixed (byte* label = MakeLabel(descriptor.Label))
        fixed (RhiTextureFormat* pViewFormats = viewFormats)
        {
            var webGpuDesc = new TextureDescriptor
            {
                SampleCount = descriptor.SampleCount,
                MipLevelCount = descriptor.MipLevelCount,
                Dimension = (TextureDimension) dimension,
                Format = (TextureFormat) format,
                Label = label,
                Size = WgpuExtent3D(descriptor.Size),
                Usage = (TextureUsage) usage,
                ViewFormats = (TextureFormat*) pViewFormats,
                ViewFormatCount = checked((uint) viewFormats.Length),
            };

            texturePtr = _webGpu.DeviceCreateTexture(_wgpuDevice, &webGpuDesc);
        }

        if (texturePtr == null)
            throw new RhiException("Texture creation failed");

        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _textureRegistry.Add(handle, new TextureReg { Native = texturePtr });
        return new RhiTexture(this, handle);
    }

    internal override RhiTextureView TextureCreateView(RhiTexture texture, in RhiTextureViewDescriptor descriptor)
    {
        // TODO: Thread safety
        var nativeTexture = _textureRegistry[texture.Handle].Native;

        var format = ValidateTextureFormat(descriptor.Format);
        var dimension = ValidateTextureViewDimension(descriptor.Dimension);
        var aspect = ValidateTextureAspect(descriptor.Aspect);

        var mipLevelCount = descriptor.MipLevelCount;
        var arrayLayerCount = descriptor.ArrayLayerCount;

        if (mipLevelCount == 0)
            throw new ArgumentException($"Invalid {nameof(descriptor.MipLevelCount)}");

        if (arrayLayerCount == 0)
            throw new ArgumentException($"Invalid {nameof(descriptor.ArrayLayerCount)}");

        TextureView* textureView;
        fixed (byte* label = MakeLabel(descriptor.Label))
        {
            var webGpuDesc = new TextureViewDescriptor
            {
                Format = (TextureFormat) format,
                Dimension = (TextureViewDimension) dimension,
                Aspect = (TextureAspect) aspect,
                Label = label,
                BaseMipLevel = descriptor.BaseMipLevel,
                MipLevelCount = mipLevelCount,
                BaseArrayLayer = descriptor.BaseArrayLayer,
                ArrayLayerCount = descriptor.ArrayLayerCount
            };

            textureView = _webGpu.TextureCreateView(nativeTexture, &webGpuDesc);
        }

        return AllocRhiTextureView(textureView);
    }

    internal override void TextureViewDrop(RhiTextureView textureView)
    {
        _wgpu.TextureViewDrop(_textureViewRegistry[textureView.Handle].Native);

        _textureViewRegistry.Remove(textureView.Handle);
    }

    internal override RhiTextureView CreateTextureViewForWindow(Clyde.WindowReg reg)
    {
        // TODO: Thread safety

        var swapChain = reg.RhiWebGpuData!.SwapChain;

        // This creates a new texture view handle.
        var textureView = _webGpu.SwapChainGetCurrentTextureView(swapChain);

        return AllocRhiTextureView(textureView);
    }

    private RhiTextureView AllocRhiTextureView(TextureView* native)
    {
        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _textureViewRegistry.Add(handle, new TextureViewReg { Native = native });
        return new RhiTextureView(this, handle);
    }

    private sealed class TextureReg
    {
        public WGPUTexture* Native;
    }

    private sealed class TextureViewReg
    {
        public TextureView* Native;
    }
}
