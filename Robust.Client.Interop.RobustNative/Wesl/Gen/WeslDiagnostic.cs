namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslDiagnostic
{
    [NativeTypeName("const char *")]
    public sbyte* file;

    [NativeTypeName("unsigned int")]
    public uint span_start;

    [NativeTypeName("unsigned int")]
    public uint span_end;

    [NativeTypeName("const char *")]
    public sbyte* title;
}
