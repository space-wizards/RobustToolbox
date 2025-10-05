namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslExecResult
{
    [NativeTypeName("_Bool")]
    public byte success;

    [NativeTypeName("const WeslBindingArray *")]
    public WeslBindingArray* resources;

    public WeslError error;
}
