using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    public class FontResource : BaseResource
    {
        public DynamicFont Font { get; private set; }
        public DynamicFontData FontData { get; private set; }

        public override void Load(IResourceCache cache, string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            var res = ResourceLoader.Load(path);
            if (!(res is DynamicFontData fontData))
            {
                throw new InvalidDataException("Path does not point to a font.");
            }

            FontData = fontData;
            Font = new DynamicFont();
            Font.AddFallback(FontData);
        }
    }
}
