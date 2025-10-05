namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUQueueWorkDoneStatus
{
    WGPUQueueWorkDoneStatus_Success = 0x00000001,
    WGPUQueueWorkDoneStatus_InstanceDropped = 0x00000002,
    WGPUQueueWorkDoneStatus_Error = 0x00000003,
    WGPUQueueWorkDoneStatus_Unknown = 0x00000004,
    WGPUQueueWorkDoneStatus_Force32 = 0x7FFFFFFF,
}
