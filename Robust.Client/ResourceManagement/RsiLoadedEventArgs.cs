using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;

namespace Robust.Client.ResourceManagement
{
    public readonly struct RsiLoadedEventArgs
    {
        internal RsiLoadedEventArgs(ResourcePath path, RSIResource resource, Image atlas, Dictionary<RSI.StateId, Vector2i[][]> atlasOffsets)
        {
            Path = path;
            Resource = resource;
            Atlas = atlas;
            AtlasOffsets = atlasOffsets;
        }

        public ResourcePath Path { get; }
        public RSIResource Resource { get; }
        public Image Atlas { get; }
        public Dictionary<RSI.StateId, Vector2i[][]> AtlasOffsets { get; }
    }
}
