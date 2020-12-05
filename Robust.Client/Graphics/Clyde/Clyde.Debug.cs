using Robust.Client.Interfaces.Graphics;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private readonly ClydeDebugStats _debugStats = new();

        private sealed class ClydeDebugInfo : IClydeDebugInfo
        {
            public ClydeDebugInfo(OpenGLVersion openGLVersion, string renderer, string vendor, string versionString, bool overriding)
            {
                OpenGLVersion = openGLVersion;
                Renderer = renderer;
                Vendor = vendor;
                VersionString = versionString;
                Overriding = overriding;
            }

            public OpenGLVersion OpenGLVersion { get; }
            public bool Overriding { get; }
            public string Renderer { get; }
            public string Vendor { get; }
            public string VersionString { get; }
        }

        private sealed class ClydeDebugStats : IClydeDebugStats
        {
            public int LastGLDrawCalls { get; set; }
            public int LastClydeDrawCalls { get; set; }
            public int LastBatches { get; set; }
            public (int vertices, int indices) LargestBatchSize => (LargestBatchVertices, LargestBatchIndices);
            public int LargestBatchVertices { get; set; }
            public int LargestBatchIndices { get; set; }
            public int TotalLights { get; set; }

            public void Reset()
            {
                LastGLDrawCalls = 0;
                LastClydeDrawCalls = 0;
                LastBatches = 0;
                LargestBatchVertices = 0;
                LargestBatchIndices = 0;
                TotalLights = 0;
            }
        }
    }
}
