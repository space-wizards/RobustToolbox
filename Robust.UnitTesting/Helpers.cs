using System;
using System.IO;
using System.Text;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.UnitTesting
{
    internal static class Helpers
    {
        public static void MountString(this IResourceManagerInternal resourceManager, string path, string content)
        {
            if (path.Contains("\n"))
            {
                throw new ArgumentException("Mount path contains newline. Did you mix up mount path and content?");
            }

            var stream = new MemoryStream();
            stream.Write(Encoding.UTF8.GetBytes(content));
            stream.Position = 0;
            resourceManager.MountStreamAt(stream, new ResourcePath(path));
        }
    }
}
