namespace Robust.Shared.Prototypes;

public readonly record struct ProtoId<T>(string Id) where T : class, IPrototype
{
    public static implicit operator string(ProtoId<T> protoId)
    {
        return protoId.Id;
    }

    public static implicit operator ProtoId<T>(string id)
    {
        return new ProtoId<T>(id);
    }
}
