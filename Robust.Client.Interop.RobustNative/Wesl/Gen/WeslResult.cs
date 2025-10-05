namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslResult
{
    [NativeTypeName("_Bool")]
    public byte success;

    [NativeTypeName("const char *")]
    public sbyte* data;

    public WeslError error;
}
