using System.IO;
using System.Text;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement.ResourceTypes
{
    internal class GodotAssetResource : BaseResource
    {
        public GodotAsset Asset { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                Asset = GodotParser.Parse(reader);
            }
        }
    }
}
