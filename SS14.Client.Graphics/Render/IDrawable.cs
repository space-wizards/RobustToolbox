namespace SS14.Client.Graphics.Render
{
    public interface IDrawable
    {
        SFML.Graphics.Drawable SFMLDrawable { get; }
        void Draw(IRenderTarget target, RenderStates states);
        void Draw();
    }
}
