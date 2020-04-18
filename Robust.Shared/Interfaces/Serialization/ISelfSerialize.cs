namespace Robust.Shared.Interfaces.Serialization
{
    public interface ISelfSerialize
    {
        void Deserialize(string value);

        string Serialize();
    }
}
