using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics.Render
{
    public interface IRenderTarget
    {
        SFML.Graphics.RenderTarget SFMLTarget { get; }
        Vector2u Size { get; }
        uint Width { get; }
        uint Height { get; }

        void Clear(Color color);
        void Draw(IDrawable drawable);
        void Draw(SFML.Graphics.Drawable drawable);
    }
}
