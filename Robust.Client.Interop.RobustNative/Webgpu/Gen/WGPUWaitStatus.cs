namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUWaitStatus
{
    WGPUWaitStatus_Success = 0x00000001,
    WGPUWaitStatus_TimedOut = 0x00000002,
    WGPUWaitStatus_UnsupportedTimeout = 0x00000003,
    WGPUWaitStatus_UnsupportedCount = 0x00000004,
    WGPUWaitStatus_UnsupportedMixedSources = 0x00000005,
    WGPUWaitStatus_Force32 = 0x7FFFFFFF,
}
