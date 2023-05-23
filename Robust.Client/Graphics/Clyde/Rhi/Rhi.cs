﻿using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Maths;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global

namespace Robust.Client.Graphics.Clyde.Rhi;

//
// The Render Hardware Interface (RHI) is an abstraction for the underlying native graphics API.
//
// The API is a copy of WebGPU, made to be safe for exposure to sandboxed content.
//

// TODO: Make sure default values make sense.
// TODO: Work around C# stupid struct parameter nonsense
// TODO: Do we really want nullables wrapping big structs?
// TODO: Actually maybe we should use nullables instead of undefined enum members?
// TODO: Is it really worth it to make every enum a byte?
// TODO: I probably don't need ROS<byte> overloads when there's a ROS<T> overload.
// TODO: Buffer mapping could be redone to allow concurrent access to independent mapped ranges.
// Requires tracking mapped ranges from the buffer object somewhere so they can have individual locks.
// TODO: Safety
// TODO: SAFETY  !!!!!

public abstract partial class RhiBase
{
    public abstract RhiQueue Queue { get; }

    public abstract RhiTexture CreateTexture(in RhiTextureDescriptor descriptor);

    public abstract RhiSampler CreateSampler(in RhiSamplerDescriptor descriptor);

    public abstract RhiShaderModule CreateShaderModule(in RhiShaderModuleDescriptor descriptor);

    public abstract RhiPipelineLayout CreatePipelineLayout(in RhiPipelineLayoutDescriptor descriptor);

    public abstract RhiRenderPipeline CreateRenderPipeline(in RhiRenderPipelineDescriptor descriptor);

    public abstract RhiCommandEncoder CreateCommandEncoder(in RhiCommandEncoderDescriptor descriptor);

    public abstract RhiBindGroupLayout CreateBindGroupLayout(in RhiBindGroupLayoutDescriptor descriptor);

    public abstract RhiBindGroup CreateBindGroup(in RhiBindGroupDescriptor descriptor);

    public abstract RhiBuffer CreateBuffer(in RhiBufferDescriptor descriptor);
}

/// <summary>
/// Values for <see cref="CVars.DisplayGpuPowerPreference"/>.
/// </summary>
public enum RhiPowerPreference : byte
{
    Undefined = 0,
    LowPower = 1,
    HighPerformance = 2,
    Final
}

public record struct RhiBufferDescriptor(
    ulong Size,
    RhiBufferUsageFlags Usage,
    bool MappedAtCreation = false,
    string? Label = null
);

public sealed class RhiBuffer : RhiObject
{
    internal ActiveMapping? Mapping;

    // public RhiBufferMapState MapState => Rhi.BufferGetMapState(this);

    public async ValueTask MapAsync(RhiMapModeFlags mode, nuint offset, nuint size)
    {
        await Rhi.BufferMapAsync(this, mode, offset, size);
    }

    public RhiMappedBufferRange GetMappedRange(nuint offset, nuint size)
    {
        return Rhi.BufferGetMappedRange(this, offset, size);
    }

    public void Unmap()
    {
        Rhi.BufferUnmap(this);
    }

    internal RhiBuffer(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }

    internal override void ReleaseUnmanagedResources()
    {
        Rhi.BufferDrop(this);
    }

    internal sealed class ActiveMapping
    {
        // Keep around for GC.
        private readonly RhiBuffer _buffer;

        // We use lock() to avoid concurrent access to the active mapping, to avoid thread safety issues.
        // lock(), however, is recursive.
        // As such we need to make sure you can't get span access (through callback)
        // and then unmap it while holding onto the span.
        internal int ActiveSpans;
        internal bool Valid;

        public ActiveMapping(RhiBuffer buffer)
        {
            _buffer = buffer;
        }
    }
}

public sealed class RhiMappedBufferRange
{
    private readonly RhiBuffer.ActiveMapping _mapping;

    private readonly unsafe void* _pData;
    private readonly int _length;

    public void Write<T>(in T data, nuint offset) where T : unmanaged
    {
        Write(new ReadOnlySpan<T>(in data), offset);
    }

    public void Write<T>(ReadOnlySpan<T> data, nuint offset) where T : unmanaged
    {
        if (offset > int.MaxValue)
            throw new ArgumentException("Offset too big");

        var byteSpan = MemoryMarshal.Cast<T, byte>(data);

        lock (_mapping)
        {
            CheckMapValid();

            byteSpan.CopyTo(DataSpan[(int) offset..]);
        }
    }

    public void Read(Span<byte> data, nuint offset)
    {
        if (offset > int.MaxValue)
            throw new ArgumentException("Offset too big");

        lock (_mapping)
        {
            CheckMapValid();

            var slice = DataSpan[(int)offset..];
            if (slice.Length > data.Length)
                slice = slice[..data.Length];

            slice.CopyTo(data);
        }
    }

    public void GetSpan<TArg>(TArg state, SpanAction<byte, TArg> action)
    {
        Monitor.Enter(_mapping);
        try
        {
            CheckMapValid();
            _mapping.ActiveSpans += 1;

            action(DataSpan, state);
        }
        finally
        {
            _mapping.ActiveSpans -= 1;
            Monitor.Exit(_mapping);
        }
    }

    private void CheckMapValid()
    {
        if (!_mapping.Valid)
            throw new InvalidOperationException("Buffer has been unmapped!");
    }

    internal unsafe RhiMappedBufferRange(RhiBuffer.ActiveMapping mapping, void* pData, int length)
    {
        _mapping = mapping;
        _pData = pData;
        _length = length;
    }

    private unsafe Span<byte> DataSpan => new(_pData, _length);
}

[Flags]
public enum RhiMapModeFlags : byte
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
}

public enum RhiBufferMapState : byte
{
    Unmapped = 0,
    Pending = 1,
    Mapped = 2
}

[Flags]
public enum RhiBufferUsageFlags : ushort
{
    None = 0,
    MapRead = 1 << 0,
    MapWrite = 1 << 1,
    CopySrc = 1 << 2,
    CopyDst = 1 << 3,
    Index = 1 << 4,
    Vertex = 1 << 5,
    Uniform = 1 << 6,
    Storage = 1 << 7,
    Indirect = 1 << 8,
    QueryResolve = 1 << 9
}

public sealed class RhiBindGroup : RhiObject
{
    internal RhiBindGroup(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }

    internal override void ReleaseUnmanagedResources()
    {
        Rhi.BindGroupDrop(this);
    }
}

public record struct RhiBindGroupDescriptor(
    RhiBindGroupLayout Layout,
    RhiBindGroupEntry[] Entries,
    string? Label = null
);

public record struct RhiBindGroupEntry(uint Binding, IRhiBindingResource Resource);

public interface IRhiBindingResource
{
}

public record struct RhiPipelineLayoutDescriptor(RhiBindGroupLayout[] BindGroupLayouts, string? Label = null);

public record struct RhiBindGroupLayoutDescriptor(RhiBindGroupLayoutEntry[] Entries, string? Label = null);

public record struct RhiBindGroupLayoutEntry(uint Binding, RhiShaderStage Visibility, RhiBindingLayout Layout);

public sealed record RhiBufferBinding(RhiBuffer Buffer, ulong Offset = 0, ulong? Size = null) : IRhiBindingResource;

public abstract record RhiBindingLayout;

public sealed record RhiTextureBindingLayout(
    RhiTextureSampleType SampleType = RhiTextureSampleType.Float,
    RhiTextureViewDimension ViewDimension = RhiTextureViewDimension.Dim2D,
    bool Multisampled = false
) : RhiBindingLayout;

public sealed record RhiSamplerBindingLayout(
    RhiSamplerBindingType Type = RhiSamplerBindingType.Filtering
) : RhiBindingLayout;

public sealed record RhiBufferBindingLayout(
    RhiBufferBindingType Type = RhiBufferBindingType.Uniform,
    bool HasDynamicOffset = false,
    ulong MinBindingSize = 0
) : RhiBindingLayout;

public enum RhiBufferBindingType : byte
{
    Undefined = 0,
    Uniform = 1,
    Storage = 2,
    ReadOnlyStorage = 3,
    Final
}

public enum RhiTextureSampleType : byte
{
    Float = 0x00000001,
    UnfilterableFloat = 0x00000002,
    Depth = 0x00000003,
    Sint = 0x00000004,
    Uint = 0x00000005,
    Final
}

[Flags]
public enum RhiShaderStage : byte
{
    None = 0,
    Vertex = 1 << 0,
    Fragment = 1 << 1,
    Compute = 1 << 2,
}

public enum RhiSamplerBindingType : byte
{
    Filtering = 1,
    NonFiltering = 2,
    Comparison = 3,
    Final
}

public sealed class RhiBindGroupLayout : RhiObject
{
    internal RhiBindGroupLayout(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public record struct RhiRenderPassDescriptor(
    RhiRenderPassColorAttachment[] ColorAttachments,
    RhiRenderPassDepthStencilAttachment? DepthStencilAttachment = null,
    RhiQuerySet? OcclusionQuerySet = null,
    ulong MaxDrawCount = 50000000,
    string? Label = null
    // TODO: Timestamp writes, can't make heads or tails of the API.
);

public sealed class RhiQuerySet : RhiObject
{
    internal RhiQuerySet(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public record struct RhiRenderPassColorAttachment(
    RhiTextureView View,
    RhiLoadOp LoadOp,
    RhiStoreOp StoreOp,
    RhiTextureView? ResolveTarget = null,
    RhiColor ClearValue = default
);

public record struct RhiColor(double R, double G, double B, double A)
{
    public static implicit operator RhiColor(Color color) => new(color.R, color.G, color.B, color.A);
}

public enum RhiLoadOp : byte
{
    Undefined = 0,
    Clear = 1,
    Load = 2,
    Final
}

public enum RhiStoreOp : byte
{
    Undefined = 0,
    Store = 1,
    Discard = 2,
    Final
}

public record struct RhiRenderPassDepthStencilAttachment(
    RhiTextureView View,
    float DepthClearValue,
    RhiLoadOp DepthLoadOp = RhiLoadOp.Undefined,
    RhiStoreOp DepthStoreOp = RhiStoreOp.Undefined,
    bool DepthReadOnly = false,
    uint StencilClearValue = 0,
    RhiLoadOp StencilLoadOp = RhiLoadOp.Undefined,
    RhiStoreOp StencilStoreOp = RhiStoreOp.Undefined,
    bool StencilReadOnly = false
);

public sealed class RhiRenderPassEncoder : RhiObject
{
    public void SetPipeline(RhiRenderPipeline pipeline)
    {
        Rhi.RenderPassEncoderSetPipeline(this, pipeline);
    }

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        Rhi.RenderPassEncoderDraw(this, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    public void SetBindGroup(uint index, RhiBindGroup? bindGroup)
    {
        Rhi.RenderPassEncoderSetBindGroup(this, index, bindGroup);
    }

    public void SetVertexBuffer(uint slot, RhiBuffer? buffer, ulong offset = 0, ulong? size = null)
    {
        Rhi.RenderPassEncoderSetVertexBuffer(this, slot, buffer, offset, size);
    }

    public void SetScissorRect(uint x, uint y, uint w, uint h)
    {
        Rhi.RenderPassEncoderSetScissorRect(this, x, y, w, h);
    }

    public void End()
    {
        Rhi.RenderPassEncoderEnd(this);
    }

    internal RhiRenderPassEncoder(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public record struct RhiCommandBufferDescriptor(
    string? Label = null
);

public sealed class RhiCommandBuffer : RhiObject
{
    internal RhiCommandBuffer(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }

    internal override void ReleaseUnmanagedResources()
    {
        Rhi.CommandBufferDrop(this);
    }
}

public record struct RhiCommandEncoderDescriptor(
    string? Label = null
);

// Command encoders are ref structs to ensure content does not access them from multiple threads concurrently.
// This allows us to avoid locking on them internally.
public sealed class RhiCommandEncoder : RhiObject
{
    public RhiRenderPassEncoder BeginRenderPass(in RhiRenderPassDescriptor descriptor)
    {
        return Rhi.CommandEncoderBeginRenderPass(this, descriptor);
    }

    public RhiCommandBuffer Finish(in RhiCommandBufferDescriptor descriptor)
    {
        return Rhi.CommandEncoderFinish(this, descriptor);
    }

    public RhiCommandBuffer Finish()
    {
        return Finish(new RhiCommandBufferDescriptor());
    }

    internal RhiCommandEncoder(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public interface IRhiPipelineLayoutBase
{
}

public sealed class RhiPipelineLayout : RhiObject, IRhiPipelineLayoutBase
{
    internal RhiPipelineLayout(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public sealed class RhiAutoLayoutMode : IRhiPipelineLayoutBase
{
    public static RhiAutoLayoutMode Instance { get; } = new();
}

public record struct RhiRenderPipelineDescriptor(
    IRhiPipelineLayoutBase? Layout,
    RhiVertexState Vertex,
    RhiPrimitiveState Primitive,
    RhiDepthStencilState? DepthStencil,
    RhiMultisampleState Multisample,
    RhiFragmentState? Fragment,
    string? Label = null
);

public record struct RhiVertexState(RhiProgrammableStage ProgrammableStage, RhiVertexBufferLayout[] Buffers)
{
    public RhiVertexState(RhiProgrammableStage programmableStage)
        : this(programmableStage, Array.Empty<RhiVertexBufferLayout>())
    {
    }
}

public record struct RhiConstantEntry(string Key, double Value);

public record struct RhiProgrammableStage(
    RhiShaderModule ShaderModule,
    string EntryPoint,
    RhiConstantEntry[] Constants
)
{
    public RhiProgrammableStage(RhiShaderModule shaderModule, string entryPoint)
        : this(shaderModule, entryPoint, Array.Empty<RhiConstantEntry>())
    {
    }
}

public record struct RhiVertexBufferLayout(
    ulong ArrayStride,
    RhiVertexStepMode StepMode,
    RhiVertexAttribute[] Attributes
);

public enum RhiVertexStepMode : byte
{
    Vertex = 0,
    Instance = 1,
    Final
}

public record struct RhiVertexAttribute(RhiVertexFormat Format, ulong Offset, uint ShaderLocation);

public enum RhiVertexFormat : byte
{
    Undefined = 0x00000000,
    Uint8x2 = 0x00000001,
    Uint8x4 = 0x00000002,
    Sint8x2 = 0x00000003,
    Sint8x4 = 0x00000004,
    Unorm8x2 = 0x00000005,
    Unorm8x4 = 0x00000006,
    Snorm8x2 = 0x00000007,
    Snorm8x4 = 0x00000008,
    Uint16x2 = 0x00000009,
    Uint16x4 = 0x0000000A,
    Sint16x2 = 0x0000000B,
    Sint16x4 = 0x0000000C,
    Unorm16x2 = 0x0000000D,
    Unorm16x4 = 0x0000000E,
    Snorm16x2 = 0x0000000F,
    Snorm16x4 = 0x00000010,
    Float16x2 = 0x00000011,
    Float16x4 = 0x00000012,
    Float32 = 0x00000013,
    Float32x2 = 0x00000014,
    Float32x3 = 0x00000015,
    Float32x4 = 0x00000016,
    Uint32 = 0x00000017,
    Uint32x2 = 0x00000018,
    Uint32x3 = 0x00000019,
    Uint32x4 = 0x0000001A,
    Sint32 = 0x0000001B,
    Sint32x2 = 0x0000001C,
    Sint32x3 = 0x0000001D,
    Sint32x4 = 0x0000001E,
    Final
}

public record struct RhiPrimitiveState(
    RhiPrimitiveTopology Topology = RhiPrimitiveTopology.TriangleList,
    RhiIndexFormat StripIndexformat = RhiIndexFormat.Undefined,
    RhiFrontFace FrontFace = RhiFrontFace.Ccw,
    RhiCullMode CullMode = RhiCullMode.None,
    bool UnclippedDepth = false
)
{
    public RhiPrimitiveState() : this(RhiPrimitiveTopology.TriangleList)
    {
    }
}

public enum RhiPrimitiveTopology : byte
{
    PointList = 0x00000000,
    LineList = 0x00000001,
    LineStrip = 0x00000002,
    TriangleList = 0x00000003,
    TriangleStrip = 0x00000004,
    Final
}

public enum RhiFrontFace : byte
{
    Ccw = 0,
    Cw = 1,
    Final
}

public enum RhiCullMode : byte
{
    None = 0,
    Front = 1,
    Back = 2,
    Final
}

public enum RhiIndexFormat : byte
{
    Undefined = 0,
    Uint16 = 1,
    Uint32 = 2,
    Final
}

public record struct RhiDepthStencilState(
    RhiTextureFormat Format,
    bool DepthWriteEnabled,
    RhiCompareFunction DepthCompare,
    //  TODO: Fix struct defs to make these not nullable please god
    RhiStencilFaceState? StencilFront,
    RhiStencilFaceState? StencilBack,
    uint StencilReadMask = 0xFFFFFF,
    uint StencilWriteMask = 0xFFFFFFFF,
    int DepthBias = 0,
    float DepthBiasSlopeScale = 0,
    float DepthBiasClamp = 0
);

public record struct RhiStencilFaceState(
    RhiCompareFunction Compare = RhiCompareFunction.Always,
    RhiStencilOperation FailOp = RhiStencilOperation.Keep,
    RhiStencilOperation DepthFailOp = RhiStencilOperation.Keep,
    RhiStencilOperation PassOp = RhiStencilOperation.Keep
)
{
    public RhiStencilFaceState() : this(RhiCompareFunction.Always)
    {
    }
}

public enum RhiStencilOperation : byte
{
    Keep = 0x00000000,
    Zero = 0x00000001,
    Replace = 0x00000002,
    Invert = 0x00000003,
    IncrementClamp = 0x00000004,
    DecrementClamp = 0x00000005,
    IncrementWrap = 0x00000006,
    DecrementWrap = 0x00000007,
    Final
}

public record struct RhiMultisampleState(
    uint Count = 1,
    uint Mask = 0xFFFFFFFF,
    bool AlphaToCoverageEnabled = false
)
{
    public RhiMultisampleState() : this(1)
    {
    }
}

public record struct RhiFragmentState(RhiProgrammableStage ProgrammableStage, RhiColorTargetState[] Targets);

public record struct RhiColorTargetState(
    RhiTextureFormat Format,
    RhiBlendState? Blend = null,
    RhiColorWriteFlags WriteMask = RhiColorWriteFlags.All
);

public record struct RhiBlendState(RhiBlendComponent Color, RhiBlendComponent Alpha)
{
    public RhiBlendState() : this(new RhiBlendComponent(), new RhiBlendComponent())
    {
    }
}

public record struct RhiBlendComponent(
    RhiBlendOperation Operation = RhiBlendOperation.Add,
    RhiBlendFactor SrcFactor = RhiBlendFactor.One,
    RhiBlendFactor DstFactor = RhiBlendFactor.Zero
)
{
    public RhiBlendComponent() : this(RhiBlendOperation.Add)
    {
    }
}

public enum RhiBlendOperation : byte
{
    Add = 0x00000000,
    Subtract = 0x00000001,
    ReverseSubtract = 0x00000002,
    Min = 0x00000003,
    Max = 0x00000004,
    Final
}

public enum RhiBlendFactor : byte
{
    Zero = 0x00000000,
    One = 0x00000001,
    Src = 0x00000002,
    OneMinusSrc = 0x00000003,
    SrcAlpha = 0x00000004,
    OneMinusSrcAlpha = 0x00000005,
    Dst = 0x00000006,
    OneMinusDst = 0x00000007,
    DstAlpha = 0x00000008,
    OneMinusDstAlpha = 0x00000009,
    SrcAlphaSaturated = 0x0000000A,
    Constant = 0x0000000B,
    OneMinusConstant = 0x0000000C,
    Final
}

[Flags]
public enum RhiColorWriteFlags : byte
{
    None = 0,
    Red = 1 << 0,
    Green = 1 << 1,
    Blue = 1 << 2,
    Alpha = 1 << 3,
    All = 0xF,
}

public record struct RhiShaderModuleDescriptor(
    // TODO: Hints
    // TODO: source map ?
    string Code,
    string? Label
);

public record struct RhiImageCopyTexture(
    RhiTexture Texture,
    uint MipLevel = 0,
    RhiOrigin3D Origin = default,
    RhiTextureAspect Aspect = default
);

public record struct RhiImageDataLayout(
    ulong Offset,
    uint BytesPerRow,
    uint RowsPerImage
);

public record struct RhiTextureDescriptor(
    RhiExtent3D Size,
    RhiTextureFormat Format,
    RhiTextureUsage Usage,
    RhiTextureDimension Dimension = RhiTextureDimension.Dim2D,
    uint MipLevelCount = 1,
    uint SampleCount = 1,
    RhiTextureFormat[]? ViewFormats = null,
    string? Label = null
);

public record struct RhiTextureViewDescriptor(
    RhiTextureFormat Format,
    RhiTextureViewDimension Dimension,
    RhiTextureAspect Aspect,
    uint BaseMipLevel,
    uint MipLevelCount,
    uint BaseArrayLayer,
    uint ArrayLayerCount,
    string? Label
);

public record struct RhiSamplerDescriptor(
    RhiAddressMode AddressModeU = RhiAddressMode.ClampToEdge,
    RhiAddressMode AddressModeV = RhiAddressMode.ClampToEdge,
    RhiAddressMode AddressModeW = RhiAddressMode.ClampToEdge,
    RhiFilterMode MagFilter = RhiFilterMode.Nearest,
    RhiFilterMode MinFilter = RhiFilterMode.Nearest,
    RhiMipmapFilterMode MipmapFilter = RhiMipmapFilterMode.Nearest,
    float LodMinClamp = 0,
    float LodMaxClamp = 32,
    RhiCompareFunction Compare = RhiCompareFunction.Undefined,
    ushort MaxAnisotropy = 1,
    string? Label = null
)
{
    public RhiSamplerDescriptor() : this(RhiAddressMode.ClampToEdge)
    {
    }
}

public enum RhiAddressMode : byte
{
    ClampToEdge = 0,
    Repeat = 1,
    MirrorRepeat = 2,
    Final
}

public enum RhiFilterMode : byte
{
    Linear = 0,
    Nearest = 1,
    Final
}

public enum RhiMipmapFilterMode : byte
{
    Linear = 0,
    Nearest = 1,
    Final
}

public enum RhiCompareFunction : byte
{
    Undefined = 0x00000000,
    Never = 0x00000001,
    Less = 0x00000002,
    LessEqual = 0x00000003,
    Greater = 0x00000004,
    GreaterEqual = 0x00000005,
    Equal = 0x00000006,
    NotEqual = 0x00000007,
    Always = 0x00000008,
    Final
}

public abstract class RhiObject : IDisposable
{
    internal readonly RhiBase Rhi;
    internal readonly RhiHandle Handle;

    private protected RhiObject(RhiBase rhi, RhiHandle handle)
    {
        Rhi = rhi;
        Handle = handle;
    }

    internal virtual void ReleaseUnmanagedResources()
    {
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~RhiObject()
    {
        ReleaseUnmanagedResources();
    }
}

// TODO: Shouldn't be a RhiObject since it can't be disposed directly?
public sealed class RhiQueue : RhiObject
{
    internal RhiQueue(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }

    public void Submit(params RhiCommandBuffer[] commandBuffers)
    {
        Rhi.QueueSubmit(this, commandBuffers);
    }

    public void WriteTexture(
        in RhiImageCopyTexture destination,
        ReadOnlySpan<byte> data,
        in RhiImageDataLayout dataLayout,
        RhiExtent3D size)
    {
        Rhi.QueueWriteTexture(this, destination, data, dataLayout, size);
    }

    public void WriteTexture<T>(
        in RhiImageCopyTexture destination,
        ReadOnlySpan<T> data,
        in RhiImageDataLayout dataLayout,
        RhiExtent3D size)
        where T : unmanaged
    {
        WriteTexture(
            destination,
            MemoryMarshal.Cast<T, byte>(data),
            dataLayout,
            size
        );
    }

    public void WriteBuffer(
        RhiBuffer buffer,
        ulong bufferOffset,
        ReadOnlySpan<byte> data)
    {
        Rhi.QueueWriteBuffer(buffer, bufferOffset, data);
    }

    public void WriteBuffer<T>(
        RhiBuffer buffer,
        ulong bufferOffset,
        ReadOnlySpan<T> data)
        where T : unmanaged
    {
        WriteBuffer(
            buffer,
            bufferOffset,
            MemoryMarshal.Cast<T, byte>(data)
        );
    }
}

public sealed class RhiTexture : RhiObject
{
    internal RhiTexture(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }

    public RhiTextureView CreateView(in RhiTextureViewDescriptor descriptor)
    {
        return Rhi.TextureCreateView(this, in descriptor);
    }
}

public sealed class RhiTextureView : RhiObject, IRhiBindingResource
{
    internal RhiTextureView(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }

    internal override void ReleaseUnmanagedResources()
    {
        Rhi.TextureViewDrop(this);
    }
}

public sealed class RhiSampler : RhiObject, IRhiBindingResource
{
    internal RhiSampler(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public sealed class RhiShaderModule : RhiObject
{
    internal RhiShaderModule(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public sealed class RhiRenderPipeline : RhiObject
{
    internal RhiRenderPipeline(RhiBase rhi, RhiHandle handle) : base(rhi, handle)
    {
    }
}

public record struct RhiExtent3D(uint Width, uint Height = 1, uint Depth = 1)
{
    public RhiExtent3D(int width, int height = 1, int depth = 1) : this((uint)width, (uint)height, (uint)depth)
    {
    }
}

public record struct RhiOrigin3D(uint X = 0, uint Y = 0, uint Z = 0)
{
    public RhiOrigin3D(int x = 0, int y = 0, int z = 0) : this((uint)x, (uint)y, (uint)z)
    {
    }
}

public enum RhiTextureDimension : byte
{
    Dim1D = 0,
    Dim2D = 1,
    Dim3D = 2,
}

public enum RhiTextureViewDimension : byte
{
    Undefined = 0,
    Dim1D = 1,
    Dim2D = 2,
    Dim2DArray = 3,
    Cube = 4,
    CubeArray = 5,
    Dim3D = 6,
    Final
}

public enum RhiTextureFormat
{
    R8Unorm = 0x00000001,
    R8Snorm = 0x00000002,
    R8Uint = 0x00000003,
    R8Sint = 0x00000004,
    R16Uint = 0x00000005,
    R16Sint = 0x00000006,
    R16Float = 0x00000007,
    RG8Unorm = 0x00000008,
    RG8Snorm = 0x00000009,
    RG8Uint = 0x0000000A,
    RG8Sint = 0x0000000B,
    R32Float = 0x0000000C,
    R32Uint = 0x0000000D,
    R32Sint = 0x0000000E,
    RG16Uint = 0x0000000F,
    RG16Sint = 0x00000010,
    RG16Float = 0x00000011,
    RGBA8Unorm = 0x00000012,
    RGBA8UnormSrgb = 0x00000013,
    RGBA8Snorm = 0x00000014,
    RGBA8Uint = 0x00000015,
    RGBA8Sint = 0x00000016,
    BGRA8Unorm = 0x00000017,
    BGRA8UnormSrgb = 0x00000018,
    RGB10A2Unorm = 0x00000019,
    RG11B10Ufloat = 0x0000001A,
    RGB9E5Ufloat = 0x0000001B,
    RG32Float = 0x0000001C,
    RG32Uint = 0x0000001D,
    RG32Sint = 0x0000001E,
    RGBA16Uint = 0x0000001F,
    RGBA16Sint = 0x00000020,
    RGBA16Float = 0x00000021,
    RGBA32Float = 0x00000022,
    RGBA32Uint = 0x00000023,
    RGBA32Sint = 0x00000024,
    Stencil8 = 0x00000025,
    Depth16Unorm = 0x00000026,
    Depth24Plus = 0x00000027,
    Depth24PlusStencil8 = 0x00000028,
    Depth32Float = 0x00000029,
    Depth32FloatStencil8 = 0x0000002A,
    BC1RGBAUnorm = 0x0000002B,
    BC1RGBAUnormSrgb = 0x0000002C,
    BC2RGBAUnorm = 0x0000002D,
    BC2RGBAUnormSrgb = 0x0000002E,
    BC3RGBAUnorm = 0x0000002F,
    BC3RGBAUnormSrgb = 0x00000030,
    BC4RUnorm = 0x00000031,
    BC4RSnorm = 0x00000032,
    BC5RGUnorm = 0x00000033,
    BC5RGSnorm = 0x00000034,
    BC6HRGBUfloat = 0x00000035,
    BC6HRGBFloat = 0x00000036,
    BC7RGBAUnorm = 0x00000037,
    BC7RGBAUnormSrgb = 0x00000038,
    ETC2RGB8Unorm = 0x00000039,
    ETC2RGB8UnormSrgb = 0x0000003A,
    ETC2RGB8A1Unorm = 0x0000003B,
    ETC2RGB8A1UnormSrgb = 0x0000003C,
    ETC2RGBA8Unorm = 0x0000003D,
    ETC2RGBA8UnormSrgb = 0x0000003E,
    EACR11Unorm = 0x0000003F,
    EACR11Snorm = 0x00000040,
    EACRG11Unorm = 0x00000041,
    EACRG11Snorm = 0x00000042,
    ASTC4x4Unorm = 0x00000043,
    ASTC4x4UnormSrgb = 0x00000044,
    ASTC5x4Unorm = 0x00000045,
    ASTC5x4UnormSrgb = 0x00000046,
    ASTC5x5Unorm = 0x00000047,
    ASTC5x5UnormSrgb = 0x00000048,
    ASTC6x5Unorm = 0x00000049,
    ASTC6x5UnormSrgb = 0x0000004A,
    ASTC6x6Unorm = 0x0000004B,
    ASTC6x6UnormSrgb = 0x0000004C,
    ASTC8x5Unorm = 0x0000004D,
    ASTC8x5UnormSrgb = 0x0000004E,
    ASTC8x6Unorm = 0x0000004F,
    ASTC8x6UnormSrgb = 0x00000050,
    ASTC8x8Unorm = 0x00000051,
    ASTC8x8UnormSrgb = 0x00000052,
    ASTC10x5Unorm = 0x00000053,
    ASTC10x5UnormSrgb = 0x00000054,
    ASTC10x6Unorm = 0x00000055,
    ASTC10x6UnormSrgb = 0x00000056,
    ASTC10x8Unorm = 0x00000057,
    ASTC10x8UnormSrgb = 0x00000058,
    ASTC10x10Unorm = 0x00000059,
    ASTC10x10UnormSrgb = 0x0000005A,
    ASTC12x10Unorm = 0x0000005B,
    ASTC12x10UnormSrgb = 0x0000005C,
    ASTC12x12Unorm = 0x0000005D,
    ASTC12x12UnormSrgb = 0x0000005E,
    Final
}

[Flags]
public enum RhiTextureUsage : byte
{
    None = 0,
    CopySrc = 1,
    CopyDst = 2,
    TextureBinding = 4,
    StorageBinding = 8,
    RenderAttachment = 16,
    Final = 32,
}

public enum RhiTextureAspect : byte
{
    All = 0,
    StencilOnly = 1,
    DepthOnly = 2,
    Final
}

[Virtual]
public class RhiException : Exception
{
    public RhiException()
    {
    }

    public RhiException(string message) : base(message)
    {
    }
}
