namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUSurfaceGetCurrentTextureStatus
{
    WGPUSurfaceGetCurrentTextureStatus_SuccessOptimal = 0x00000001,
    WGPUSurfaceGetCurrentTextureStatus_SuccessSuboptimal = 0x00000002,
    WGPUSurfaceGetCurrentTextureStatus_Timeout = 0x00000003,
    WGPUSurfaceGetCurrentTextureStatus_Outdated = 0x00000004,
    WGPUSurfaceGetCurrentTextureStatus_Lost = 0x00000005,
    WGPUSurfaceGetCurrentTextureStatus_OutOfMemory = 0x00000006,
    WGPUSurfaceGetCurrentTextureStatus_DeviceLost = 0x00000007,
    WGPUSurfaceGetCurrentTextureStatus_Error = 0x00000008,
    WGPUSurfaceGetCurrentTextureStatus_Force32 = 0x7FFFFFFF,
}
