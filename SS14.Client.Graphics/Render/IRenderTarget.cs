using SS14.Client.Graphics.Views;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Render
{
    public interface IRenderTarget
    {
        SFML.Graphics.RenderTarget SFMLTarget { get; }
        Vector2u Size { get; }
        uint Width { get; }
        uint Height { get; }

        View View { get; set; }

        void Clear(Color color);
        void Draw(IDrawable drawable);
        // This has to have its own name vs overload
        // Because the C# overload detector refuses to resolve anything
        // if a single overload can't potentially be resolved due to unreferenced assemblies
        // Thus, if this were an overload
        // Draw() would be unusable from SS14.Client, as it doesn't have SFML.Graphics.
        // Yes, this is absolutely retarded.
        void DrawSFML(SFML.Graphics.Drawable drawable);
    }
}
