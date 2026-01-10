namespace Robust.Shared.Interop.RobustNative.Ogg;

internal unsafe partial struct ogg_iovec_t
{
    public void* iov_base;

    [NativeTypeName("size_t")]
    public nuint iov_len;
}
