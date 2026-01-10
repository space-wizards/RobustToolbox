namespace Robust.Shared.Interop.RobustNative.Ogg;

internal unsafe partial struct ogg_sync_state
{
    [NativeTypeName("unsigned char *")]
    public byte* data;

    public int storage;

    public int fill;

    public int returned;

    public int unsynced;

    public int headerbytes;

    public int bodybytes;
}
