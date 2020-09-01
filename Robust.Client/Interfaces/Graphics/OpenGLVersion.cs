namespace Robust.Client.Interfaces.Graphics
{
    public readonly struct OpenGLVersion
    {
        public readonly byte Major;
        public readonly byte Minor;
        public readonly bool IsES;
        public readonly bool IsCore;

        public OpenGLVersion(byte major, byte minor, bool isES, bool isCore)
        {
            Major = major;
            Minor = minor;
            IsES = isES;
            IsCore = isCore;
        }
    }
}
