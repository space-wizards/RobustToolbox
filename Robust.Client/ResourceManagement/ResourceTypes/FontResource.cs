using System;
using System.IO;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement.ResourceTypes
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

        public override void Dispose()
        {
            if (GameController.OnGodot)
            {
                FontData.Dispose();
            }
        }
    }
}
