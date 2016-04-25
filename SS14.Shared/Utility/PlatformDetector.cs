using System;
using System.IO;

namespace SS14.Shared.Utility
{
    public enum Platform {
        Windows,
        Linux,
        Mac
    }

    public class PlatformDetector {
        public static Platform DetectPlatform() {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return Platform.Mac;
                    else
                        return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }
    }
}
