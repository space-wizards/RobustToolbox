namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUPowerPreference
{
    WGPUPowerPreference_Undefined = 0x00000000,
    WGPUPowerPreference_LowPower = 0x00000001,
    WGPUPowerPreference_HighPerformance = 0x00000002,
    WGPUPowerPreference_Force32 = 0x7FFFFFFF,
}
