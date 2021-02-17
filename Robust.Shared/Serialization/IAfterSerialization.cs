namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Provides a method that gets executed after serialization is complete
    /// </summary>
    public interface IAfterSerialization
    {
        /// <summary>
        /// Gets executed after serialization is complete
        /// </summary>
        void AfterSerialization();
    }
}
