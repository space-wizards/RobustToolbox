namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslBindingArray
{
    [NativeTypeName("const WeslBinding *")]
    public WeslBinding* items;

    [NativeTypeName("size_t")]
    public nuint len;
}
