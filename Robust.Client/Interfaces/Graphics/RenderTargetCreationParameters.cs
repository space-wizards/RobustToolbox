namespace Robust.Client.Interfaces.Graphics
{
    public struct RenderTargetFormatParameters
    {
        public RenderTargetColorFormat ColorFormat { get; set; }
        public bool HasDepthStencil { get; set; }

        public RenderTargetFormatParameters(RenderTargetColorFormat colorFormat, bool hasDepthStencil = false)
        {
            ColorFormat = colorFormat;
            HasDepthStencil = hasDepthStencil;
        }

        public static implicit operator RenderTargetFormatParameters(RenderTargetColorFormat colorFormat)
        {
            return new(colorFormat);
        }
    }
}
