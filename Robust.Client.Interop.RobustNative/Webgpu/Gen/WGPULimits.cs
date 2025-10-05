namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPULimits
{
    public WGPUChainedStructOut* nextInChain;

    [NativeTypeName("uint32_t")]
    public uint maxTextureDimension1D;

    [NativeTypeName("uint32_t")]
    public uint maxTextureDimension2D;

    [NativeTypeName("uint32_t")]
    public uint maxTextureDimension3D;

    [NativeTypeName("uint32_t")]
    public uint maxTextureArrayLayers;

    [NativeTypeName("uint32_t")]
    public uint maxBindGroups;

    [NativeTypeName("uint32_t")]
    public uint maxBindGroupsPlusVertexBuffers;

    [NativeTypeName("uint32_t")]
    public uint maxBindingsPerBindGroup;

    [NativeTypeName("uint32_t")]
    public uint maxDynamicUniformBuffersPerPipelineLayout;

    [NativeTypeName("uint32_t")]
    public uint maxDynamicStorageBuffersPerPipelineLayout;

    [NativeTypeName("uint32_t")]
    public uint maxSampledTexturesPerShaderStage;

    [NativeTypeName("uint32_t")]
    public uint maxSamplersPerShaderStage;

    [NativeTypeName("uint32_t")]
    public uint maxStorageBuffersPerShaderStage;

    [NativeTypeName("uint32_t")]
    public uint maxStorageTexturesPerShaderStage;

    [NativeTypeName("uint32_t")]
    public uint maxUniformBuffersPerShaderStage;

    [NativeTypeName("uint64_t")]
    public ulong maxUniformBufferBindingSize;

    [NativeTypeName("uint64_t")]
    public ulong maxStorageBufferBindingSize;

    [NativeTypeName("uint32_t")]
    public uint minUniformBufferOffsetAlignment;

    [NativeTypeName("uint32_t")]
    public uint minStorageBufferOffsetAlignment;

    [NativeTypeName("uint32_t")]
    public uint maxVertexBuffers;

    [NativeTypeName("uint64_t")]
    public ulong maxBufferSize;

    [NativeTypeName("uint32_t")]
    public uint maxVertexAttributes;

    [NativeTypeName("uint32_t")]
    public uint maxVertexBufferArrayStride;

    [NativeTypeName("uint32_t")]
    public uint maxInterStageShaderVariables;

    [NativeTypeName("uint32_t")]
    public uint maxColorAttachments;

    [NativeTypeName("uint32_t")]
    public uint maxColorAttachmentBytesPerSample;

    [NativeTypeName("uint32_t")]
    public uint maxComputeWorkgroupStorageSize;

    [NativeTypeName("uint32_t")]
    public uint maxComputeInvocationsPerWorkgroup;

    [NativeTypeName("uint32_t")]
    public uint maxComputeWorkgroupSizeX;

    [NativeTypeName("uint32_t")]
    public uint maxComputeWorkgroupSizeY;

    [NativeTypeName("uint32_t")]
    public uint maxComputeWorkgroupSizeZ;

    [NativeTypeName("uint32_t")]
    public uint maxComputeWorkgroupsPerDimension;
}
