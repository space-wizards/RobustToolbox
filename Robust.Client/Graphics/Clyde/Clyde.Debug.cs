using System;
using Robust.Client.Interfaces.Graphics;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private ClydeDebugStats _debugStats;

        private sealed class ClydeDebugInfo : IClydeDebugInfo
        {
            public ClydeDebugInfo(Version openGLVersion, Version minimumVersion, string renderer, string vendor,
                string versionString)
            {
                OpenGLVersion = openGLVersion;
                MinimumVersion = minimumVersion;
                Renderer = renderer;
                Vendor = vendor;
                VersionString = versionString;
            }

            public Version OpenGLVersion { get; }
            public Version MinimumVersion { get; }
            public string Renderer { get; }
            public string Vendor { get; }
            public string VersionString { get; }
        }

        private sealed class ClydeDebugStats : IClydeDebugStats
        {
            public int LastGLDrawCalls { get; set; }
            public int LastClydeDrawCalls { get; set; }
            public int LastBatches { get; set; }

            public void Reset()
            {
                LastGLDrawCalls = 0;
                LastClydeDrawCalls = 0;
                LastBatches = 0;
            }
        }
    }
}
