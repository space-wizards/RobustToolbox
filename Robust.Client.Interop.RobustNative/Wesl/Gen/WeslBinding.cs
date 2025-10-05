namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslBinding
{
    [NativeTypeName("unsigned int")]
    public uint group;

    [NativeTypeName("unsigned int")]
    public uint binding;

    public WeslBindingType kind;

    [NativeTypeName("const uint8_t *")]
    public byte* data;

    [NativeTypeName("size_t")]
    public nuint data_len;
}
