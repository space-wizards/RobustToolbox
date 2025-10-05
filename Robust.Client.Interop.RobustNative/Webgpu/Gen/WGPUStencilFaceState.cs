namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUStencilFaceState
{
    public WGPUCompareFunction compare;

    public WGPUStencilOperation failOp;

    public WGPUStencilOperation depthFailOp;

    public WGPUStencilOperation passOp;
}
