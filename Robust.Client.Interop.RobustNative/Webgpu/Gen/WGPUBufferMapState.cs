namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUBufferMapState
{
    WGPUBufferMapState_Unmapped = 0x00000001,
    WGPUBufferMapState_Pending = 0x00000002,
    WGPUBufferMapState_Mapped = 0x00000003,
    WGPUBufferMapState_Force32 = 0x7FFFFFFF,
}
