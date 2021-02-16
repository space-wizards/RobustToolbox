namespace Robust.Shared.Serialization.Markdown
{
    public interface IDataNodeFactory
    {
        IValueDataNode GetValueNode(string value);

        IMappingDataNode GetMappingNode();

        ISequenceDataNode GetSequenceNode();
    }
}
