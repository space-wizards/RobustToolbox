namespace Robust.Shared.Interop.RobustNative.Vorbis;

internal unsafe partial struct vorbis_comment
{
    [NativeTypeName("char **")]
    public sbyte** user_comments;

    public int* comment_lengths;

    public int comments;

    [NativeTypeName("char *")]
    public sbyte* vendor;
}
