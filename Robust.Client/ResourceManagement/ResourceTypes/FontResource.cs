using System.IO;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement
{
    public sealed class FontResource : BaseResource
    {
        internal IFontFaceHandle FontFaceHandle { get; private set; } = default!;

        public override void Load(IDependencyCollection dependencies, ResPath path)
        {

            if (!dependencies.Resolve<IResourceManager>().TryContentFileRead(path, out var stream))
            {
                throw new FileNotFoundException("Content file does not exist for font");
            }

            using (stream)
            {
                FontFaceHandle = dependencies.Resolve<IFontManagerInternal>().Load(stream);
            }
        }

        public VectorFont MakeDefault()
        {
            return new(this, 12);
        }
    }
}
