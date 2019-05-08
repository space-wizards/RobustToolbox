using Robust.Client.Interfaces.ResourceManagement;
using System.IO;
using Robust.Shared.Utility;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;

namespace Robust.Client.ResourceManagement
{
    public class FontResource : BaseResource
    {
        internal IFontFaceHandle FontFaceHandle { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for font");
            }

            FontFaceHandle = IoCManager.Resolve<IFontManagerInternal>().Load(cache.ContentFileRead(path).ToArray());
        }

        public VectorFont MakeDefault()
        {
            return new VectorFont(this, 12);
        }
    }
}
