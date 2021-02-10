namespace Robust.Client.Graphics.Interfaces.Graphics
{
    internal interface IClydeDebugInfo
    {
        OpenGLVersion OpenGLVersion { get; }

        string Renderer { get; }
        string Vendor { get; }
        string VersionString { get; }
        bool Overriding { get; }
    }
}
