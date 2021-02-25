namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeCopier<TType>
    {
        TType Copy(TType source, TType target);
    }
}
