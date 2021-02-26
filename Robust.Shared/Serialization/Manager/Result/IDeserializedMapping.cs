namespace Robust.Shared.Serialization.Manager.Result
{
    // TODO Paul remove this for something saner
    public interface IDeserializedMapping
    {
        DeserializedFieldEntry[] Mapping { get; }
    }
}
