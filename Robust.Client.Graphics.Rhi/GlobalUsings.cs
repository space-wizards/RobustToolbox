global using Robust.Client.Interop.RobustNative.Webgpu;

global using static Robust.Client.Interop.RobustNative.Webgpu.Wgpu;
global using static Robust.Shared.Utility.FfiHelper;

global using unsafe WGPUTexture = Robust.Client.Interop.RobustNative.Webgpu.WGPUTextureImpl*;
global using unsafe WGPUDevice = Robust.Client.Interop.RobustNative.Webgpu.WGPUDeviceImpl*;
global using unsafe WGPUQueue = Robust.Client.Interop.RobustNative.Webgpu.WGPUQueueImpl*;
global using unsafe WGPUAdapter = Robust.Client.Interop.RobustNative.Webgpu.WGPUAdapterImpl*;
global using unsafe WGPUInstance = Robust.Client.Interop.RobustNative.Webgpu.WGPUInstanceImpl*;
global using unsafe WGPUTextureView = Robust.Client.Interop.RobustNative.Webgpu.WGPUTextureViewImpl*;
global using unsafe WGPUBindGroup = Robust.Client.Interop.RobustNative.Webgpu.WGPUBindGroupImpl*;
global using unsafe WGPUBindGroupLayout = Robust.Client.Interop.RobustNative.Webgpu.WGPUBindGroupLayoutImpl*;
global using unsafe WGPUBuffer = Robust.Client.Interop.RobustNative.Webgpu.WGPUBufferImpl*;
global using unsafe WGPUSampler = Robust.Client.Interop.RobustNative.Webgpu.WGPUSamplerImpl*;
global using unsafe WGPUCommandBuffer = Robust.Client.Interop.RobustNative.Webgpu.WGPUCommandBufferImpl*;
global using unsafe WGPUCommandEncoder = Robust.Client.Interop.RobustNative.Webgpu.WGPUCommandEncoderImpl*;
global using unsafe WGPURenderPassEncoder = Robust.Client.Interop.RobustNative.Webgpu.WGPURenderPassEncoderImpl*;
global using unsafe WGPURenderPipeline = Robust.Client.Interop.RobustNative.Webgpu.WGPURenderPipelineImpl*;
global using unsafe WGPUPipelineLayout = Robust.Client.Interop.RobustNative.Webgpu.WGPUPipelineLayoutImpl*;
global using unsafe WGPUShaderModule = Robust.Client.Interop.RobustNative.Webgpu.WGPUShaderModuleImpl*;
global using unsafe WGPUSurface = Robust.Client.Interop.RobustNative.Webgpu.WGPUSurfaceImpl*;
