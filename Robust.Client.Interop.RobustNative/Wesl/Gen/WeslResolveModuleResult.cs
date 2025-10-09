namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslResolveModuleResult
{
    [NativeTypeName("_Bool")]
    public byte success;

    public WeslTranslationUnit* module;
}
