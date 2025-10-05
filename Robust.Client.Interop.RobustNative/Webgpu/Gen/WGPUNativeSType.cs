namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUNativeSType
{
    WGPUSType_DeviceExtras = 0x00030001,
    WGPUSType_NativeLimits = 0x00030002,
    WGPUSType_PipelineLayoutExtras = 0x00030003,
    WGPUSType_ShaderSourceGLSL = 0x00030004,
    WGPUSType_InstanceExtras = 0x00030006,
    WGPUSType_BindGroupEntryExtras = 0x00030007,
    WGPUSType_BindGroupLayoutEntryExtras = 0x00030008,
    WGPUSType_QuerySetDescriptorExtras = 0x00030009,
    WGPUSType_SurfaceConfigurationExtras = 0x0003000A,
    WGPUSType_SurfaceSourceSwapChainPanel = 0x0003000B,
    WGPUNativeSType_Force32 = 0x7FFFFFFF,
}
