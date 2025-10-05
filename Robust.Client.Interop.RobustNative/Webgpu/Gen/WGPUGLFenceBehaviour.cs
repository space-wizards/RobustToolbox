namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUGLFenceBehaviour
{
    WGPUGLFenceBehaviour_Normal = 0x00000000,
    WGPUGLFenceBehaviour_AutoFinish = 0x00000001,
    WGPUGLFenceBehaviour_Force32 = 0x7FFFFFFF,
}
