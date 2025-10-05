namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUCompilationInfoRequestStatus
{
    WGPUCompilationInfoRequestStatus_Success = 0x00000001,
    WGPUCompilationInfoRequestStatus_InstanceDropped = 0x00000002,
    WGPUCompilationInfoRequestStatus_Error = 0x00000003,
    WGPUCompilationInfoRequestStatus_Unknown = 0x00000004,
    WGPUCompilationInfoRequestStatus_Force32 = 0x7FFFFFFF,
}
