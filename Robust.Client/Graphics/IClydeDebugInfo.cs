namespace Robust.Client.Graphics
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
