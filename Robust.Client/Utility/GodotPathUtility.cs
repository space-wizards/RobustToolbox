using System;
using Robust.Shared.Utility;

namespace Robust.Client.Utility
{
    public static class GodotPathUtility
    {
        public static ResourcePath GodotPathToResourcePath(string path)
        {
            const string resContent = "res://Content";
            if (path.StartsWith(resContent))
            {
                return new ResourcePath(path.Substring(resContent.Length));
            }

            const string resEngine = "res://Engine";
            if (path.StartsWith(resEngine))
            {
                return new ResourcePath(path.Substring(resEngine.Length));
            }

            throw new ArgumentException("Cannot map Godot path to resource path.");
        }
    }
}
