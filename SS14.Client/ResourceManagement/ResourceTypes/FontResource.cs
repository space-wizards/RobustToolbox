using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;
using SS14.Shared.Utility;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.IoC;

namespace SS14.Client.ResourceManagement
{
    public class FontResource : BaseResource
    {
        internal Godot.DynamicFontData FontData { get; private set; }
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
                    break;
                case GameController.DisplayMode.Godot:
                    if (!cache.TryGetDiskFilePath(path, out string diskPath))
                    {
                        throw new InvalidOperationException("Fonts can only be loaded from disk.");
                    }

                    var res = Godot.ResourceLoader.Load(diskPath);
                    if (!(res is Godot.DynamicFontData fontData))
                    {
                        throw new InvalidDataException("Path does not point to a font.");
                    }

                    FontData = fontData;
                    break;
                case GameController.DisplayMode.OpenGL:
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

        public override void Dispose()
        {
            if (GameController.OnGodot)
            {
                FontData.Dispose();
            }
        }
    }
}
