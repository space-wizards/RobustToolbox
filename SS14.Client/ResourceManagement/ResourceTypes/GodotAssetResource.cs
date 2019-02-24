using System.IO;
using System.Text;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.Utility;

namespace SS14.Client.ResourceManagement.ResourceTypes
{
    internal class GodotAssetResource : BaseResource
    {
        public GodotAsset Asset { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                Asset = GodotParser.Parse(reader);
            }
        }
    }
}
