namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUPopErrorScopeStatus
{
    WGPUPopErrorScopeStatus_Success = 0x00000001,
    WGPUPopErrorScopeStatus_InstanceDropped = 0x00000002,
    WGPUPopErrorScopeStatus_EmptyStack = 0x00000003,
    WGPUPopErrorScopeStatus_Force32 = 0x7FFFFFFF,
}
