namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslParseResult
{
    [NativeTypeName("_Bool")]
    public byte success;

    public WeslTranslationUnit* data;

    public WeslError error;
}
