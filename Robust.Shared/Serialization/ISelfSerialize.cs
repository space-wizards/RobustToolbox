namespace Robust.Shared.Serialization
{
    public interface ISelfSerialize
    {
        void Deserialize(string value);

        string Serialize();
    }
}
