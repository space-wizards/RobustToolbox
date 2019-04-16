using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Graphics
{
    public interface IDirectionalTextureProvider
    {
        Texture Default { get; }
        Texture TextureFor(Direction dir);
    }
}
