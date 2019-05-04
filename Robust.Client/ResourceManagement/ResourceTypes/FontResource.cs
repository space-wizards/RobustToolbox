using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                case GameController.DisplayMode.Clyde:
                    FontFaceHandle = IoCManager.Resolve<IFontManagerInternal>().Load(cache.ContentFileRead(path).ToArray());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        public VectorFont MakeDefault()
        {
            return new VectorFont(this, 12);
        }
    }
}
