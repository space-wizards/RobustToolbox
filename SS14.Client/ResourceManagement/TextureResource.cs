using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.ResourceManagement;

namespace SS14.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public Texture Texture => Texture;
        private StreamTexture texture;

        public override void Load(IResourceCache cache, string path, Stream stream)
        {
            // TODO: Optimize this maybe? Right now it has to sync to disk.
        }

        public override void Dispose()
        {
        }
    }
}
