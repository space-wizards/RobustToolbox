using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;
using SS14.Shared.Utility;
using SS14.Client.Graphics;

namespace SS14.Client.ResourceManagement
{
    public class FontResource : BaseResource
    {
        #if GODOT
        public Godot.DynamicFontData FontData { get; private set; }
        #endif

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            #if GODOT
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for texture");
            }
            if (!cache.TryGetDiskFilePath(path, out string diskPath))
            {
                throw new InvalidOperationException("Textures can only be loaded from disk.");
            }

            var res = Godot.ResourceLoader.Load(diskPath);
            if (!(res is Godot.DynamicFontData fontData))
            {
                throw new InvalidDataException("Path does not point to a font.");
            }

            FontData = fontData;
            #endif
        }

        public VectorFont MakeDefault()
        {
            return new VectorFont(this)
            {
                Size = 12,
            };
        }

        public override void Dispose()
        {
            #if GODOT
            FontData.Dispose();
            FontData = null;
            #endif
        }
    }
}
