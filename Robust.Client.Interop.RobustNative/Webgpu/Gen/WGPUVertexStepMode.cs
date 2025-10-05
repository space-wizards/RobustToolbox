namespace Robust.Client.Interop.RobustNative.Webgpu;

internal enum WGPUVertexStepMode
{
    WGPUVertexStepMode_VertexBufferNotUsed = 0x00000000,
    WGPUVertexStepMode_Undefined = 0x00000001,
    WGPUVertexStepMode_Vertex = 0x00000002,
    WGPUVertexStepMode_Instance = 0x00000003,
    WGPUVertexStepMode_Force32 = 0x7FFFFFFF,
}
