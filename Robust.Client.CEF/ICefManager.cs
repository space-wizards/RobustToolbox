namespace Robust.Client.CEF
{
    public interface ICefManager
    {
        void Initialize();

        void CheckInitialized();

        void Update();

        void Shutdown();
    }
}
