using System;
using System.IO;

namespace SS14.Shared.Utility
{
    public class PlatformTools
    {
        public static string SanePath(String pathname)
        {
            // sanitize the pathname in config.xml for the current OS
            // N.B.: You cannot use Path.DirectorySeparatorChar and
            // Path.AltDirectorySeparatorChar here, because on some platforms
            // they are the same.  Mono/linux has both as '/', for example.
            // Hardcode the only platforms we care about.

            var separators = new char[] { '/', '\\' };
            string newpath = "";
            foreach (string tmp in pathname.Split(separators))
                newpath = Path.Combine(newpath, tmp);
            return newpath;
        }
    }
}

