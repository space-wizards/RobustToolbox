using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeCopier<TType>
    {
        [MustUseReturnValue]
        TType Copy(TType source, TType target);
    }
}
