using System.Diagnostics;

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, TextureReg> _textureRegistry = new();
    private readonly Dictionary<RhiHandle, TextureViewReg> _textureViewRegistry = new();

    public override RhiTexture CreateTexture(in RhiTextureDescriptor descriptor)
    {
        var format = descriptor.Format;
        var usage = descriptor.Usage;
        ValidateTextureFormat(format);
        var dimension = ValidateTextureDimension(descriptor.Dimension);
        ValidateTextureUsage(usage);

        // TODO: Copy to stackalloc instead.
        var viewFormats = descriptor.ViewFormats?.ToArray() ?? [];
        foreach (var vf in viewFormats)
        {
            ValidateTextureFormat(vf);
        }

        Debug.Assert(
            sizeof(RhiTextureFormat) == sizeof(WGPUTextureFormat),
            "Pointer to view formats array is cast directly to pass to native, sizes must match");

        WGPUTexture texturePtr;
        var label = MakeLabel(descriptor.Label);
        fixed (byte* pLabel = label)
        fixed (RhiTextureFormat* pViewFormats = viewFormats)
        {
            var webGpuDesc = new WGPUTextureDescriptor
            {
                sampleCount = descriptor.SampleCount,
                mipLevelCount = descriptor.MipLevelCount,
                dimension = dimension,
                format = (WGPUTextureFormat) format,
                label = new WGPUStringView
                {
                    data = (sbyte*)pLabel,
                    length = (UIntPtr)(label?.Length ?? 0),
                },
                size = WgpuExtent3D(descriptor.Size),
                usage = (ulong) usage,
                viewFormats = (WGPUTextureFormat*) pViewFormats,
                viewFormatCount = checked((uint) viewFormats.Length),
            };

            texturePtr = wgpuDeviceCreateTexture(_wgpuDevice, &webGpuDesc);
        }

        if (texturePtr == null)
            throw new RhiException("Texture creation failed");

        return AllocRhiTexture(texturePtr);
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

        WGPUTextureView textureView;
        fixed (byte* label = MakeLabel(descriptor.Label))
        {
            var webGpuDesc = new WGPUTextureViewDescriptor
            {
                format = (WGPUTextureFormat) format,
                dimension = (WGPUTextureViewDimension) dimension,
                aspect = (WGPUTextureAspect) aspect,
                label = new WGPUStringView
                {
                    data = (sbyte*)label,
                    length = WGPU_STRLEN
                },
                baseMipLevel = descriptor.BaseMipLevel,
                mipLevelCount = mipLevelCount,
                baseArrayLayer = descriptor.BaseArrayLayer,
                arrayLayerCount = descriptor.ArrayLayerCount
            };

            textureView = wgpuTextureCreateView(nativeTexture, &webGpuDesc);
        }

        return AllocRhiTextureView(textureView);
    }

    internal override void TextureViewDrop(RhiTextureView textureView)
    {
        wgpuTextureViewRelease(_textureViewRegistry[textureView.Handle].Native);

        _textureViewRegistry.Remove(textureView.Handle);
    }

    internal override RhiTexture GetSurfaceTextureForWindow(WindowData reg)
    {
        // TODO: Thread safety

        var surface = reg.Surface;

        // This creates a new texture view handle.
        WGPUSurfaceTexture textureRet;
        wgpuSurfaceGetCurrentTexture(surface, &textureRet);

        return AllocRhiTexture(textureRet.texture);
    }

    private RhiTexture AllocRhiTexture(WGPUTexture native)
    {
        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _textureRegistry.Add(handle, new TextureReg { Native = native });
        return new RhiTexture(this, handle);
    }

    private RhiTextureView AllocRhiTextureView(WGPUTextureView native)
    {
        // TODO: Thread safety
        var handle = AllocRhiHandle();
        _textureViewRegistry.Add(handle, new TextureViewReg { Native = native });
        return new RhiTextureView(this, handle);
    }

    private sealed class TextureReg
    {
        public WGPUTexture Native;
    }

    private sealed class TextureViewReg
    {
        public WGPUTextureView Native;
    }
}
