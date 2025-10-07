namespace Robust.Shared.Utility;

/// <summary>
/// Pointer-wrapper struct so pointers can be sanely stored in generics and records.
/// </summary>
/// <typeparam name="T">The actual type pointed to</typeparam>
internal unsafe struct Ptr<T> where T : unmanaged
{
    public T* P;

    public static implicit operator T*(Ptr<T> t) => t.P;
    public static implicit operator Ptr<T>(T* ptr) => new() { P = ptr };
}
