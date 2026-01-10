namespace Robust.Shared.Interop.RobustNative.Vorbis;

internal unsafe partial struct alloc_chain
{
    public void* ptr;

    [NativeTypeName("struct alloc_chain *")]
    public alloc_chain* next;
}
