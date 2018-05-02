using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    public interface IDirectionalTextureProvider
    {
        Texture Default { get; }
        Texture TextureFor(Direction dir);
    }
}
