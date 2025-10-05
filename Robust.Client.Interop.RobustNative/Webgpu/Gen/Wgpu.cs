using System.Runtime.InteropServices;

namespace Robust.Client.Interop.RobustNative.Webgpu;

internal static unsafe partial class Wgpu
{
    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_None = 0x0000000000000000;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_MapRead = 0x0000000000000001;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_MapWrite = 0x0000000000000002;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_CopySrc = 0x0000000000000004;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_CopyDst = 0x0000000000000008;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_Index = 0x0000000000000010;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_Vertex = 0x0000000000000020;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_Uniform = 0x0000000000000040;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_Storage = 0x0000000000000080;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_Indirect = 0x0000000000000100;

    [NativeTypeName("const WGPUBufferUsage")]
    public const ulong WGPUBufferUsage_QueryResolve = 0x0000000000000200;

    [NativeTypeName("const WGPUColorWriteMask")]
    public const ulong WGPUColorWriteMask_None = 0x0000000000000000;

    [NativeTypeName("const WGPUColorWriteMask")]
    public const ulong WGPUColorWriteMask_Red = 0x0000000000000001;

    [NativeTypeName("const WGPUColorWriteMask")]
    public const ulong WGPUColorWriteMask_Green = 0x0000000000000002;

    [NativeTypeName("const WGPUColorWriteMask")]
    public const ulong WGPUColorWriteMask_Blue = 0x0000000000000004;

    [NativeTypeName("const WGPUColorWriteMask")]
    public const ulong WGPUColorWriteMask_Alpha = 0x0000000000000008;

    [NativeTypeName("const WGPUColorWriteMask")]
    public const ulong WGPUColorWriteMask_All = 0x000000000000000F;

    [NativeTypeName("const WGPUMapMode")]
    public const ulong WGPUMapMode_None = 0x0000000000000000;

    [NativeTypeName("const WGPUMapMode")]
    public const ulong WGPUMapMode_Read = 0x0000000000000001;

    [NativeTypeName("const WGPUMapMode")]
    public const ulong WGPUMapMode_Write = 0x0000000000000002;

    [NativeTypeName("const WGPUShaderStage")]
    public const ulong WGPUShaderStage_None = 0x0000000000000000;

    [NativeTypeName("const WGPUShaderStage")]
    public const ulong WGPUShaderStage_Vertex = 0x0000000000000001;

    [NativeTypeName("const WGPUShaderStage")]
    public const ulong WGPUShaderStage_Fragment = 0x0000000000000002;

    [NativeTypeName("const WGPUShaderStage")]
    public const ulong WGPUShaderStage_Compute = 0x0000000000000004;

    [NativeTypeName("const WGPUTextureUsage")]
    public const ulong WGPUTextureUsage_None = 0x0000000000000000;

    [NativeTypeName("const WGPUTextureUsage")]
    public const ulong WGPUTextureUsage_CopySrc = 0x0000000000000001;

    [NativeTypeName("const WGPUTextureUsage")]
    public const ulong WGPUTextureUsage_CopyDst = 0x0000000000000002;

    [NativeTypeName("const WGPUTextureUsage")]
    public const ulong WGPUTextureUsage_TextureBinding = 0x0000000000000004;

    [NativeTypeName("const WGPUTextureUsage")]
    public const ulong WGPUTextureUsage_StorageBinding = 0x0000000000000008;

    [NativeTypeName("const WGPUTextureUsage")]
    public const ulong WGPUTextureUsage_RenderAttachment = 0x0000000000000010;

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUInstance")]
    public static extern WGPUInstanceImpl* wgpuCreateInstance([NativeTypeName("const WGPUInstanceDescriptor *")] WGPUInstanceDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuGetInstanceCapabilities(WGPUInstanceCapabilities* capabilities);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUProc")]
    public static extern delegate* unmanaged[Cdecl]<void> wgpuGetProcAddress(WGPUStringView procName);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuAdapterGetFeatures([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter, WGPUSupportedFeatures* features);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuAdapterGetInfo([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter, WGPUAdapterInfo* info);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuAdapterGetLimits([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter, WGPULimits* limits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBool")]
    public static extern uint wgpuAdapterHasFeature([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter, WGPUFeatureName feature);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuAdapterRequestDevice([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter, [NativeTypeName("const WGPUDeviceDescriptor *")] WGPUDeviceDescriptor* descriptor, WGPURequestDeviceCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuAdapterAddRef([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuAdapterRelease([NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuAdapterInfoFreeMembers(WGPUAdapterInfo adapterInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBindGroupSetLabel([NativeTypeName("WGPUBindGroup")] WGPUBindGroupImpl* bindGroup, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBindGroupAddRef([NativeTypeName("WGPUBindGroup")] WGPUBindGroupImpl* bindGroup);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBindGroupRelease([NativeTypeName("WGPUBindGroup")] WGPUBindGroupImpl* bindGroup);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBindGroupLayoutSetLabel([NativeTypeName("WGPUBindGroupLayout")] WGPUBindGroupLayoutImpl* bindGroupLayout, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBindGroupLayoutAddRef([NativeTypeName("WGPUBindGroupLayout")] WGPUBindGroupLayoutImpl* bindGroupLayout);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBindGroupLayoutRelease([NativeTypeName("WGPUBindGroupLayout")] WGPUBindGroupLayoutImpl* bindGroupLayout);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBufferDestroy([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const void *")]
    public static extern void* wgpuBufferGetConstMappedRange([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("size_t")] nuint offset, [NativeTypeName("size_t")] nuint size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUBufferMapState wgpuBufferGetMapState([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void* wgpuBufferGetMappedRange([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("size_t")] nuint offset, [NativeTypeName("size_t")] nuint size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint64_t")]
    public static extern ulong wgpuBufferGetSize([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBufferUsage")]
    public static extern ulong wgpuBufferGetUsage([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuBufferMapAsync([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("WGPUMapMode")] ulong mode, [NativeTypeName("size_t")] nuint offset, [NativeTypeName("size_t")] nuint size, WGPUBufferMapCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBufferSetLabel([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBufferUnmap([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBufferAddRef([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuBufferRelease([NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandBufferSetLabel([NativeTypeName("WGPUCommandBuffer")] WGPUCommandBufferImpl* commandBuffer, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandBufferAddRef([NativeTypeName("WGPUCommandBuffer")] WGPUCommandBufferImpl* commandBuffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandBufferRelease([NativeTypeName("WGPUCommandBuffer")] WGPUCommandBufferImpl* commandBuffer);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUComputePassEncoder")]
    public static extern WGPUComputePassEncoderImpl* wgpuCommandEncoderBeginComputePass([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("const WGPUComputePassDescriptor *")] WGPUComputePassDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPURenderPassEncoder")]
    public static extern WGPURenderPassEncoderImpl* wgpuCommandEncoderBeginRenderPass([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("const WGPURenderPassDescriptor *")] WGPURenderPassDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderClearBuffer([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint64_t")] ulong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderCopyBufferToBuffer([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* source, [NativeTypeName("uint64_t")] ulong sourceOffset, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* destination, [NativeTypeName("uint64_t")] ulong destinationOffset, [NativeTypeName("uint64_t")] ulong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderCopyBufferToTexture([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("const WGPUTexelCopyBufferInfo *")] WGPUTexelCopyBufferInfo* source, [NativeTypeName("const WGPUTexelCopyTextureInfo *")] WGPUTexelCopyTextureInfo* destination, [NativeTypeName("const WGPUExtent3D *")] WGPUExtent3D* copySize);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderCopyTextureToBuffer([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("const WGPUTexelCopyTextureInfo *")] WGPUTexelCopyTextureInfo* source, [NativeTypeName("const WGPUTexelCopyBufferInfo *")] WGPUTexelCopyBufferInfo* destination, [NativeTypeName("const WGPUExtent3D *")] WGPUExtent3D* copySize);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderCopyTextureToTexture([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("const WGPUTexelCopyTextureInfo *")] WGPUTexelCopyTextureInfo* source, [NativeTypeName("const WGPUTexelCopyTextureInfo *")] WGPUTexelCopyTextureInfo* destination, [NativeTypeName("const WGPUExtent3D *")] WGPUExtent3D* copySize);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUCommandBuffer")]
    public static extern WGPUCommandBufferImpl* wgpuCommandEncoderFinish([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("const WGPUCommandBufferDescriptor *")] WGPUCommandBufferDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderInsertDebugMarker([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, WGPUStringView markerLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderPopDebugGroup([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderPushDebugGroup([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, WGPUStringView groupLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderResolveQuerySet([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, [NativeTypeName("uint32_t")] uint firstQuery, [NativeTypeName("uint32_t")] uint queryCount, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* destination, [NativeTypeName("uint64_t")] ulong destinationOffset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderSetLabel([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderWriteTimestamp([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder, [NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, [NativeTypeName("uint32_t")] uint queryIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderAddRef([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuCommandEncoderRelease([NativeTypeName("WGPUCommandEncoder")] WGPUCommandEncoderImpl* commandEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderDispatchWorkgroups([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, [NativeTypeName("uint32_t")] uint workgroupCountX, [NativeTypeName("uint32_t")] uint workgroupCountY, [NativeTypeName("uint32_t")] uint workgroupCountZ);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderDispatchWorkgroupsIndirect([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* indirectBuffer, [NativeTypeName("uint64_t")] ulong indirectOffset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderEnd([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderInsertDebugMarker([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, WGPUStringView markerLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderPopDebugGroup([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderPushDebugGroup([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, WGPUStringView groupLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderSetBindGroup([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, [NativeTypeName("uint32_t")] uint groupIndex, [NativeTypeName("WGPUBindGroup")] WGPUBindGroupImpl* group, [NativeTypeName("size_t")] nuint dynamicOffsetCount, [NativeTypeName("const uint32_t *")] uint* dynamicOffsets);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderSetLabel([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderSetPipeline([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, [NativeTypeName("WGPUComputePipeline")] WGPUComputePipelineImpl* pipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderAddRef([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderRelease([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBindGroupLayout")]
    public static extern WGPUBindGroupLayoutImpl* wgpuComputePipelineGetBindGroupLayout([NativeTypeName("WGPUComputePipeline")] WGPUComputePipelineImpl* computePipeline, [NativeTypeName("uint32_t")] uint groupIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePipelineSetLabel([NativeTypeName("WGPUComputePipeline")] WGPUComputePipelineImpl* computePipeline, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePipelineAddRef([NativeTypeName("WGPUComputePipeline")] WGPUComputePipelineImpl* computePipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePipelineRelease([NativeTypeName("WGPUComputePipeline")] WGPUComputePipelineImpl* computePipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBindGroup")]
    public static extern WGPUBindGroupImpl* wgpuDeviceCreateBindGroup([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUBindGroupDescriptor *")] WGPUBindGroupDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBindGroupLayout")]
    public static extern WGPUBindGroupLayoutImpl* wgpuDeviceCreateBindGroupLayout([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUBindGroupLayoutDescriptor *")] WGPUBindGroupLayoutDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBuffer")]
    public static extern WGPUBufferImpl* wgpuDeviceCreateBuffer([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUBufferDescriptor *")] WGPUBufferDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUCommandEncoder")]
    public static extern WGPUCommandEncoderImpl* wgpuDeviceCreateCommandEncoder([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUCommandEncoderDescriptor *")] WGPUCommandEncoderDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUComputePipeline")]
    public static extern WGPUComputePipelineImpl* wgpuDeviceCreateComputePipeline([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUComputePipelineDescriptor *")] WGPUComputePipelineDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuDeviceCreateComputePipelineAsync([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUComputePipelineDescriptor *")] WGPUComputePipelineDescriptor* descriptor, WGPUCreateComputePipelineAsyncCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUPipelineLayout")]
    public static extern WGPUPipelineLayoutImpl* wgpuDeviceCreatePipelineLayout([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUPipelineLayoutDescriptor *")] WGPUPipelineLayoutDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUQuerySet")]
    public static extern WGPUQuerySetImpl* wgpuDeviceCreateQuerySet([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUQuerySetDescriptor *")] WGPUQuerySetDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPURenderBundleEncoder")]
    public static extern WGPURenderBundleEncoderImpl* wgpuDeviceCreateRenderBundleEncoder([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPURenderBundleEncoderDescriptor *")] WGPURenderBundleEncoderDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPURenderPipeline")]
    public static extern WGPURenderPipelineImpl* wgpuDeviceCreateRenderPipeline([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPURenderPipelineDescriptor *")] WGPURenderPipelineDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuDeviceCreateRenderPipelineAsync([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPURenderPipelineDescriptor *")] WGPURenderPipelineDescriptor* descriptor, WGPUCreateRenderPipelineAsyncCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUSampler")]
    public static extern WGPUSamplerImpl* wgpuDeviceCreateSampler([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUSamplerDescriptor *")] WGPUSamplerDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUShaderModule")]
    public static extern WGPUShaderModuleImpl* wgpuDeviceCreateShaderModule([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUShaderModuleDescriptor *")] WGPUShaderModuleDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUTexture")]
    public static extern WGPUTextureImpl* wgpuDeviceCreateTexture([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUTextureDescriptor *")] WGPUTextureDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuDeviceDestroy([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUAdapterInfo wgpuDeviceGetAdapterInfo([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuDeviceGetFeatures([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, WGPUSupportedFeatures* features);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuDeviceGetLimits([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, WGPULimits* limits);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuDeviceGetLostFuture([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUQueue")]
    public static extern WGPUQueueImpl* wgpuDeviceGetQueue([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBool")]
    public static extern uint wgpuDeviceHasFeature([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, WGPUFeatureName feature);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuDevicePopErrorScope([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, WGPUPopErrorScopeCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuDevicePushErrorScope([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, WGPUErrorFilter filter);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuDeviceSetLabel([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuDeviceAddRef([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuDeviceRelease([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUSurface")]
    public static extern WGPUSurfaceImpl* wgpuInstanceCreateSurface([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, [NativeTypeName("const WGPUSurfaceDescriptor *")] WGPUSurfaceDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuInstanceGetWGSLLanguageFeatures([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, WGPUSupportedWGSLLanguageFeatures* features);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBool")]
    public static extern uint wgpuInstanceHasWGSLLanguageFeature([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, WGPUWGSLLanguageFeatureName feature);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuInstanceProcessEvents([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuInstanceRequestAdapter([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, [NativeTypeName("const WGPURequestAdapterOptions *")] WGPURequestAdapterOptions* options, WGPURequestAdapterCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUWaitStatus wgpuInstanceWaitAny([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, [NativeTypeName("size_t")] nuint futureCount, WGPUFutureWaitInfo* futures, [NativeTypeName("uint64_t")] ulong timeoutNS);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuInstanceAddRef([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuInstanceRelease([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuPipelineLayoutSetLabel([NativeTypeName("WGPUPipelineLayout")] WGPUPipelineLayoutImpl* pipelineLayout, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuPipelineLayoutAddRef([NativeTypeName("WGPUPipelineLayout")] WGPUPipelineLayoutImpl* pipelineLayout);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuPipelineLayoutRelease([NativeTypeName("WGPUPipelineLayout")] WGPUPipelineLayoutImpl* pipelineLayout);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQuerySetDestroy([NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuQuerySetGetCount([NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUQueryType wgpuQuerySetGetType([NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQuerySetSetLabel([NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQuerySetAddRef([NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQuerySetRelease([NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuQueueOnSubmittedWorkDone([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue, WGPUQueueWorkDoneCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQueueSetLabel([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQueueSubmit([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue, [NativeTypeName("size_t")] nuint commandCount, [NativeTypeName("const WGPUCommandBuffer *")] WGPUCommandBufferImpl** commands);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQueueWriteBuffer([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong bufferOffset, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQueueWriteTexture([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue, [NativeTypeName("const WGPUTexelCopyTextureInfo *")] WGPUTexelCopyTextureInfo* destination, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint dataSize, [NativeTypeName("const WGPUTexelCopyBufferLayout *")] WGPUTexelCopyBufferLayout* dataLayout, [NativeTypeName("const WGPUExtent3D *")] WGPUExtent3D* writeSize);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQueueAddRef([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuQueueRelease([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleSetLabel([NativeTypeName("WGPURenderBundle")] WGPURenderBundleImpl* renderBundle, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleAddRef([NativeTypeName("WGPURenderBundle")] WGPURenderBundleImpl* renderBundle);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleRelease([NativeTypeName("WGPURenderBundle")] WGPURenderBundleImpl* renderBundle);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderDraw([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("uint32_t")] uint vertexCount, [NativeTypeName("uint32_t")] uint instanceCount, [NativeTypeName("uint32_t")] uint firstVertex, [NativeTypeName("uint32_t")] uint firstInstance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderDrawIndexed([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("uint32_t")] uint indexCount, [NativeTypeName("uint32_t")] uint instanceCount, [NativeTypeName("uint32_t")] uint firstIndex, [NativeTypeName("int32_t")] int baseVertex, [NativeTypeName("uint32_t")] uint firstInstance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderDrawIndexedIndirect([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* indirectBuffer, [NativeTypeName("uint64_t")] ulong indirectOffset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderDrawIndirect([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* indirectBuffer, [NativeTypeName("uint64_t")] ulong indirectOffset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPURenderBundle")]
    public static extern WGPURenderBundleImpl* wgpuRenderBundleEncoderFinish([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("const WGPURenderBundleDescriptor *")] WGPURenderBundleDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderInsertDebugMarker([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, WGPUStringView markerLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderPopDebugGroup([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderPushDebugGroup([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, WGPUStringView groupLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderSetBindGroup([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("uint32_t")] uint groupIndex, [NativeTypeName("WGPUBindGroup")] WGPUBindGroupImpl* group, [NativeTypeName("size_t")] nuint dynamicOffsetCount, [NativeTypeName("const uint32_t *")] uint* dynamicOffsets);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderSetIndexBuffer([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, WGPUIndexFormat format, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint64_t")] ulong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderSetLabel([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderSetPipeline([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("WGPURenderPipeline")] WGPURenderPipelineImpl* pipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderSetVertexBuffer([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder, [NativeTypeName("uint32_t")] uint slot, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint64_t")] ulong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderAddRef([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderRelease([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* renderBundleEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderBeginOcclusionQuery([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint queryIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderDraw([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint vertexCount, [NativeTypeName("uint32_t")] uint instanceCount, [NativeTypeName("uint32_t")] uint firstVertex, [NativeTypeName("uint32_t")] uint firstInstance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderDrawIndexed([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint indexCount, [NativeTypeName("uint32_t")] uint instanceCount, [NativeTypeName("uint32_t")] uint firstIndex, [NativeTypeName("int32_t")] int baseVertex, [NativeTypeName("uint32_t")] uint firstInstance);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderDrawIndexedIndirect([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* indirectBuffer, [NativeTypeName("uint64_t")] ulong indirectOffset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderDrawIndirect([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* indirectBuffer, [NativeTypeName("uint64_t")] ulong indirectOffset);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderEnd([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderEndOcclusionQuery([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderExecuteBundles([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("size_t")] nuint bundleCount, [NativeTypeName("const WGPURenderBundle *")] WGPURenderBundleImpl** bundles);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderInsertDebugMarker([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, WGPUStringView markerLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderPopDebugGroup([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderPushDebugGroup([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, WGPUStringView groupLabel);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetBindGroup([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint groupIndex, [NativeTypeName("WGPUBindGroup")] WGPUBindGroupImpl* group, [NativeTypeName("size_t")] nuint dynamicOffsetCount, [NativeTypeName("const uint32_t *")] uint* dynamicOffsets);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetBlendConstant([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("const WGPUColor *")] WGPUColor* color);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetIndexBuffer([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, WGPUIndexFormat format, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint64_t")] ulong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetLabel([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetPipeline([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("WGPURenderPipeline")] WGPURenderPipelineImpl* pipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetScissorRect([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint x, [NativeTypeName("uint32_t")] uint y, [NativeTypeName("uint32_t")] uint width, [NativeTypeName("uint32_t")] uint height);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetStencilReference([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint reference);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetVertexBuffer([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("uint32_t")] uint slot, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint64_t")] ulong size);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetViewport([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, float x, float y, float width, float height, float minDepth, float maxDepth);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderAddRef([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderRelease([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBindGroupLayout")]
    public static extern WGPUBindGroupLayoutImpl* wgpuRenderPipelineGetBindGroupLayout([NativeTypeName("WGPURenderPipeline")] WGPURenderPipelineImpl* renderPipeline, [NativeTypeName("uint32_t")] uint groupIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPipelineSetLabel([NativeTypeName("WGPURenderPipeline")] WGPURenderPipelineImpl* renderPipeline, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPipelineAddRef([NativeTypeName("WGPURenderPipeline")] WGPURenderPipelineImpl* renderPipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPipelineRelease([NativeTypeName("WGPURenderPipeline")] WGPURenderPipelineImpl* renderPipeline);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSamplerSetLabel([NativeTypeName("WGPUSampler")] WGPUSamplerImpl* sampler, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSamplerAddRef([NativeTypeName("WGPUSampler")] WGPUSamplerImpl* sampler);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSamplerRelease([NativeTypeName("WGPUSampler")] WGPUSamplerImpl* sampler);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUFuture wgpuShaderModuleGetCompilationInfo([NativeTypeName("WGPUShaderModule")] WGPUShaderModuleImpl* shaderModule, WGPUCompilationInfoCallbackInfo callbackInfo);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuShaderModuleSetLabel([NativeTypeName("WGPUShaderModule")] WGPUShaderModuleImpl* shaderModule, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuShaderModuleAddRef([NativeTypeName("WGPUShaderModule")] WGPUShaderModuleImpl* shaderModule);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuShaderModuleRelease([NativeTypeName("WGPUShaderModule")] WGPUShaderModuleImpl* shaderModule);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSupportedFeaturesFreeMembers(WGPUSupportedFeatures supportedFeatures);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSupportedWGSLLanguageFeaturesFreeMembers(WGPUSupportedWGSLLanguageFeatures supportedWGSLLanguageFeatures);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceConfigure([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface, [NativeTypeName("const WGPUSurfaceConfiguration *")] WGPUSurfaceConfiguration* config);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuSurfaceGetCapabilities([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface, [NativeTypeName("WGPUAdapter")] WGPUAdapterImpl* adapter, WGPUSurfaceCapabilities* capabilities);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceGetCurrentTexture([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface, WGPUSurfaceTexture* surfaceTexture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUStatus wgpuSurfacePresent([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceSetLabel([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceUnconfigure([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceAddRef([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceRelease([NativeTypeName("WGPUSurface")] WGPUSurfaceImpl* surface);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSurfaceCapabilitiesFreeMembers(WGPUSurfaceCapabilities surfaceCapabilities);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUTextureView")]
    public static extern WGPUTextureViewImpl* wgpuTextureCreateView([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture, [NativeTypeName("const WGPUTextureViewDescriptor *")] WGPUTextureViewDescriptor* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureDestroy([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuTextureGetDepthOrArrayLayers([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUTextureDimension wgpuTextureGetDimension([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WGPUTextureFormat wgpuTextureGetFormat([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuTextureGetHeight([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuTextureGetMipLevelCount([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuTextureGetSampleCount([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUTextureUsage")]
    public static extern ulong wgpuTextureGetUsage([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuTextureGetWidth([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureSetLabel([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureAddRef([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureRelease([NativeTypeName("WGPUTexture")] WGPUTextureImpl* texture);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureViewSetLabel([NativeTypeName("WGPUTextureView")] WGPUTextureViewImpl* textureView, WGPUStringView label);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureViewAddRef([NativeTypeName("WGPUTextureView")] WGPUTextureViewImpl* textureView);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuTextureViewRelease([NativeTypeName("WGPUTextureView")] WGPUTextureViewImpl* textureView);

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_All = 0x00000000;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_Vulkan = 1 << 0;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_GL = 1 << 1;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_Metal = 1 << 2;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_DX12 = 1 << 3;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_DX11 = 1 << 4;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_BrowserWebGPU = 1 << 5;

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_Primary = (1 << 0) | (1 << 2) | (1 << 3) | (1 << 5);

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_Secondary = (1 << 1) | (1 << 4);

    [NativeTypeName("const WGPUInstanceBackend")]
    public const ulong WGPUInstanceBackend_Force32 = 0x7FFFFFFF;

    [NativeTypeName("const WGPUInstanceFlag")]
    public const ulong WGPUInstanceFlag_Default = 0x00000000;

    [NativeTypeName("const WGPUInstanceFlag")]
    public const ulong WGPUInstanceFlag_Debug = 1 << 0;

    [NativeTypeName("const WGPUInstanceFlag")]
    public const ulong WGPUInstanceFlag_Validation = 1 << 1;

    [NativeTypeName("const WGPUInstanceFlag")]
    public const ulong WGPUInstanceFlag_DiscardHalLabels = 1 << 2;

    [NativeTypeName("const WGPUInstanceFlag")]
    public const ulong WGPUInstanceFlag_Force32 = 0x7FFFFFFF;

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuGenerateReport([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, WGPUGlobalReport* report);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint wgpuInstanceEnumerateAdapters([NativeTypeName("WGPUInstance")] WGPUInstanceImpl* instance, [NativeTypeName("const WGPUInstanceEnumerateAdapterOptions *")] WGPUInstanceEnumerateAdapterOptions* options, [NativeTypeName("WGPUAdapter *")] WGPUAdapterImpl** adapters);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUSubmissionIndex")]
    public static extern ulong wgpuQueueSubmitForIndex([NativeTypeName("WGPUQueue")] WGPUQueueImpl* queue, [NativeTypeName("size_t")] nuint commandCount, [NativeTypeName("const WGPUCommandBuffer *")] WGPUCommandBufferImpl** commands);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUBool")]
    public static extern uint wgpuDevicePoll([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("WGPUBool")] uint wait, [NativeTypeName("const WGPUSubmissionIndex *")] ulong* submissionIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("WGPUShaderModule")]
    public static extern WGPUShaderModuleImpl* wgpuDeviceCreateShaderModuleSpirV([NativeTypeName("WGPUDevice")] WGPUDeviceImpl* device, [NativeTypeName("const WGPUShaderModuleDescriptorSpirV *")] WGPUShaderModuleDescriptorSpirV* descriptor);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSetLogCallback([NativeTypeName("WGPULogCallback")] delegate* unmanaged[Cdecl]<WGPULogLevel, WGPUStringView, void*, void> callback, void* userdata);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuSetLogLevel(WGPULogLevel level);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint wgpuGetVersion();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderSetPushConstants([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* encoder, [NativeTypeName("WGPUShaderStage")] ulong stages, [NativeTypeName("uint32_t")] uint offset, [NativeTypeName("uint32_t")] uint sizeBytes, [NativeTypeName("const void *")] void* data);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderSetPushConstants([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* encoder, [NativeTypeName("uint32_t")] uint offset, [NativeTypeName("uint32_t")] uint sizeBytes, [NativeTypeName("const void *")] void* data);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderBundleEncoderSetPushConstants([NativeTypeName("WGPURenderBundleEncoder")] WGPURenderBundleEncoderImpl* encoder, [NativeTypeName("WGPUShaderStage")] ulong stages, [NativeTypeName("uint32_t")] uint offset, [NativeTypeName("uint32_t")] uint sizeBytes, [NativeTypeName("const void *")] void* data);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderMultiDrawIndirect([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* encoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint32_t")] uint count);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderMultiDrawIndexedIndirect([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* encoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("uint32_t")] uint count);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderMultiDrawIndirectCount([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* encoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* count_buffer, [NativeTypeName("uint64_t")] ulong count_buffer_offset, [NativeTypeName("uint32_t")] uint max_count);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderMultiDrawIndexedIndirectCount([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* encoder, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* buffer, [NativeTypeName("uint64_t")] ulong offset, [NativeTypeName("WGPUBuffer")] WGPUBufferImpl* count_buffer, [NativeTypeName("uint64_t")] ulong count_buffer_offset, [NativeTypeName("uint32_t")] uint max_count);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderBeginPipelineStatisticsQuery([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, [NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, [NativeTypeName("uint32_t")] uint queryIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderEndPipelineStatisticsQuery([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderBeginPipelineStatisticsQuery([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, [NativeTypeName("uint32_t")] uint queryIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderEndPipelineStatisticsQuery([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuComputePassEncoderWriteTimestamp([NativeTypeName("WGPUComputePassEncoder")] WGPUComputePassEncoderImpl* computePassEncoder, [NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, [NativeTypeName("uint32_t")] uint queryIndex);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wgpuRenderPassEncoderWriteTimestamp([NativeTypeName("WGPURenderPassEncoder")] WGPURenderPassEncoderImpl* renderPassEncoder, [NativeTypeName("WGPUQuerySet")] WGPUQuerySetImpl* querySet, [NativeTypeName("uint32_t")] uint queryIndex);
}
