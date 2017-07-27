using System.IO;
using SFML.Graphics;
using SS14.Client.Resources;

namespace SS14.Client.ResourceManagement
{
    public class FontResource : BaseResource
    {
        public override string Fallback => @"Fonts/bluehigh.ttf";

        public Font Font { get; private set; }

        public override void Load(ResourceCache cache, string path, MemoryStream stream)
        {
            Font = new Font(stream);
        }

        public override void Dispose()
        {
            Font.Dispose();
            Font = null;
        }
    }
}
