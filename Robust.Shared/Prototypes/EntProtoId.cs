namespace Robust.Shared.Prototypes;

public readonly record struct EntProtoId(string Id)
{
    public static implicit operator string(EntProtoId protoId)
    {
        return protoId.Id;
    }

    public static implicit operator EntProtoId(string id)
    {
        return new EntProtoId(id);
    }
}
