namespace Robust.Shared.Prototypes
{
    public interface IComponentData
    {
        string[] Tags { get; }

        /// <summary>
        /// gets the mapped value of the given key. null if not mapped. exception if not in datadefinition (no corresponding [YamlField])
        /// </summary>
        object? GetValue(string key);

        /// <summary>
        /// sets the mapped value of a given key. exception if not in datadefinition (no corresponsing [YamlField])
        /// </summary>
        void SetValue(string key, object? value);
    }
}
