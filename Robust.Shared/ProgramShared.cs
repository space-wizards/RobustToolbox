using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Shared
{
    internal static class ProgramShared
    {
#if !FULL_RELEASE
        private static string FindContentRootDir(bool contentStart)
        {
            return contentStart ? "../../" : "../../../";
        }
#endif

        internal static void DoMounts(IResourceManagerInternal res, MountOptions? options, string contentBuildDir, bool loader=false, bool contentStart=false)
        {
#if FULL_RELEASE
            if (!loader)
                res.MountContentDirectory(@"Resources/");
#else
            var contentRootDir = FindContentRootDir(contentStart);
            res.MountContentDirectory($@"{contentRootDir}RobustToolbox/Resources/");
            res.MountContentDirectory($@"{contentRootDir}bin/{contentBuildDir}/", new ResourcePath("/Assemblies/"));
            res.MountContentDirectory($@"{contentRootDir}Resources/");
#endif

            if (options == null)
                return;

            foreach (var diskPath in options.DirMounts)
            {
                res.MountContentDirectory(diskPath);
            }

            foreach (var diskPath in options.ZipMounts)
            {
                res.MountContentPack(diskPath);
            }
        }
    }
}
