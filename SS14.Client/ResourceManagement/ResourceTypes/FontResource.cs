using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;
using SS14.Shared.Utility;

namespace SS14.Client.ResourceManagement
{
    public class FontResource : BaseResource
    {
        public DynamicFont Font { get; private set; }
        public DynamicFontData FontData { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for texture");
            }
            if (!cache.TryGetDiskFilePath(path, out string diskPath))
            {
                throw new InvalidOperationException("Textures can only be loaded from disk.");
            }

            var res = ResourceLoader.Load(diskPath);
            if (!(res is DynamicFontData fontData))
            {
                throw new InvalidDataException("Path does not point to a font.");
            }

            FontData = fontData;
            Font = new DynamicFont();
            Font.AddFallback(FontData);
        }

        public override void Dispose()
        {
            Font.Dispose();
            Font = null;

            FontData.Dispose();
            FontData = null;
        }
    }
}
