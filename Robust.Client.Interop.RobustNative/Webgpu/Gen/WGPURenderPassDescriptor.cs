namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPURenderPassDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("size_t")]
    public nuint colorAttachmentCount;

    [NativeTypeName("const WGPURenderPassColorAttachment *")]
    public WGPURenderPassColorAttachment* colorAttachments;

    [NativeTypeName("const WGPURenderPassDepthStencilAttachment *")]
    public WGPURenderPassDepthStencilAttachment* depthStencilAttachment;

    [NativeTypeName("WGPUQuerySet")]
    public WGPUQuerySetImpl* occlusionQuerySet;

    [NativeTypeName("const WGPURenderPassTimestampWrites *")]
    public WGPURenderPassTimestampWrites* timestampWrites;
}
