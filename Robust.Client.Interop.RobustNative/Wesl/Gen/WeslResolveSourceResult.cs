namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslResolveSourceResult
{
    [NativeTypeName("_Bool")]
    public byte success;

    [NativeTypeName("const char *")]
    public sbyte* source;
}
