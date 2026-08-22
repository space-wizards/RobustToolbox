namespace Robust.Client.Console
{
    /// <summary>
    ///     Client manager for server side scripting.
    /// </summary>
    [NotContentImplementable]
    public interface IScriptClient
    {
        void Initialize();

        bool CanScript { get; }
        void StartSession();
    }
}
