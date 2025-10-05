namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBindGroupLayoutEntry
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("uint32_t")]
    public uint binding;

    [NativeTypeName("WGPUShaderStage")]
    public ulong visibility;

    public WGPUBufferBindingLayout buffer;

    public WGPUSamplerBindingLayout sampler;

    public WGPUTextureBindingLayout texture;

    public WGPUStorageTextureBindingLayout storageTexture;
}
