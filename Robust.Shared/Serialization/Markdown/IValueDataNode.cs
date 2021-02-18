namespace Robust.Shared.Serialization.Markdown
{
    public interface IValueDataNode : IDataNode
    {
        string Value { get; }

        public string GetValue();
    }
}
