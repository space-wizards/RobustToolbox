using System.IO;
using Robust.Shared.ContentPack;

namespace Robust.Shared
{
    internal static class ProgramShared
    {
#if !FULL_RELEASE
        internal static string FindContentRootDir()
        {
            var contentPath = PathHelpers.ExecutableRelativeFile("Content.Shared.dll");

            // If Content.Shared.dll is next to us,
            // that means we've been executed from one of content's bin directories.
            if (File.Exists(contentPath))
            {
                return "../../";
            }

            return "../../../";
        }
#endif
    }
}
