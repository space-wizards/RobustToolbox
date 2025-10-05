namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUCompilationMessageType
{
    WGPUCompilationMessageType_Error = 0x00000001,
    WGPUCompilationMessageType_Warning = 0x00000002,
    WGPUCompilationMessageType_Info = 0x00000003,
    WGPUCompilationMessageType_Force32 = 0x7FFFFFFF,
}
