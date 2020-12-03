using System.Collections.Generic;

namespace Robust.Shared
{
    internal sealed class MountOptions
    {
        public List<string> ZipMounts = new();
        public List<string> DirMounts = new();
    }
}
