namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUStoreOp
{
    WGPUStoreOp_Undefined = 0x00000000,
    WGPUStoreOp_Store = 0x00000001,
    WGPUStoreOp_Discard = 0x00000002,
    WGPUStoreOp_Force32 = 0x7FFFFFFF,
}
