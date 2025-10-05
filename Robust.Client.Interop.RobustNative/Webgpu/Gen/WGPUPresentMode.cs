namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUPresentMode
{
    WGPUPresentMode_Undefined = 0x00000000,
    WGPUPresentMode_Fifo = 0x00000001,
    WGPUPresentMode_FifoRelaxed = 0x00000002,
    WGPUPresentMode_Immediate = 0x00000003,
    WGPUPresentMode_Mailbox = 0x00000004,
    WGPUPresentMode_Force32 = 0x7FFFFFFF,
}
