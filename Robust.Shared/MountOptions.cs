using System.Collections.Generic;

namespace Robust.Shared
{
    public sealed class MountOptions
    {
        public List<string> ZipMounts = new();
        public List<string> DirMounts = new();

        public MountOptions()
        { }

        public MountOptions(List<string> zipMounts, List<string> dirMounts)
        {
            ZipMounts = zipMounts;
            DirMounts = dirMounts;
        }

        public static MountOptions Merge(MountOptions a, MountOptions b)
        {
            var zipMounts = new List<string>();
            var dirMounts = new List<string>();

            zipMounts.AddRange(a.ZipMounts);
            zipMounts.AddRange(b.ZipMounts);

            dirMounts.AddRange(a.DirMounts);
            dirMounts.AddRange(b.DirMounts);

            return new MountOptions(zipMounts, dirMounts);
        }
    }
}
