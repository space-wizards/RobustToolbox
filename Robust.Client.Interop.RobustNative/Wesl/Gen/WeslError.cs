namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslError
{
    [NativeTypeName("const char *")]
    public sbyte* source;

    [NativeTypeName("const char *")]
    public sbyte* message;

    [NativeTypeName("const WeslDiagnostic *")]
    public WeslDiagnostic* diagnostics;

    [NativeTypeName("size_t")]
    public nuint diagnostics_len;
}
