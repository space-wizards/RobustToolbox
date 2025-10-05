namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUQueryType
{
    WGPUQueryType_Occlusion = 0x00000001,
    WGPUQueryType_Timestamp = 0x00000002,
    WGPUQueryType_Force32 = 0x7FFFFFFF,
}
