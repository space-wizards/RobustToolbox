namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslCompileOptions
{
    public WeslManglerKind mangler;

    [NativeTypeName("_Bool")]
    public byte sourcemap;

    [NativeTypeName("_Bool")]
    public byte imports;

    [NativeTypeName("_Bool")]
    public byte condcomp;

    [NativeTypeName("_Bool")]
    public byte generics;

    [NativeTypeName("_Bool")]
    public byte strip;

    [NativeTypeName("_Bool")]
    public byte lower;

    [NativeTypeName("_Bool")]
    public byte validate;

    [NativeTypeName("_Bool")]
    public byte naga;

    [NativeTypeName("_Bool")]
    public byte lazy;

    [NativeTypeName("_Bool")]
    public byte keep_root;

    [NativeTypeName("_Bool")]
    public byte mangle_root;

    public WeslResolverOptions* resolver;
}
