namespace Robust.Shared.Serialization;

public interface ISerializationGenerated<T> where T : ISerializationGenerated<T>
{
    public T Copy();
}
