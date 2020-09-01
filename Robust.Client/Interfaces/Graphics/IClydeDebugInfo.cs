namespace Robust.Client.Interfaces.Graphics
{
    internal interface IClydeDebugInfo
    {
        OpenGLVersion OpenGLVersion { get; }

        string Renderer { get; }
        string Vendor { get; }
        string VersionString { get; }
    }
}
