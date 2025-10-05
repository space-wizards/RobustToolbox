namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslExecOptions
{
    public WeslCompileOptions compile;

    [NativeTypeName("const char *")]
    public sbyte* entrypoint;

    [NativeTypeName("const WeslBindingArray *")]
    public WeslBindingArray* resources;

    [NativeTypeName("const WeslStringMap *")]
    public WeslStringMap* overrides;
}
