namespace Robust.Client.Graphics
{
    public interface IRsiStateLike : IDirectionalTextureProvider
    {
        RSI.State.DirectionType Directions { get; }
        bool IsAnimated { get; }
        int AnimationFrameCount { get; }

        float GetDelay(int frame);
        Texture GetFrame(RSI.State.Direction dir, int frame);
    }
}
