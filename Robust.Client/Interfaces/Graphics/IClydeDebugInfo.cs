using System;

namespace Robust.Client.Interfaces.Graphics
{
    internal interface IClydeDebugInfo
    {
        Version OpenGLVersion { get; }
        Version MinimumVersion { get; }

        string Renderer { get; }
        string Vendor { get; }
        string VersionString { get; }
    }
}