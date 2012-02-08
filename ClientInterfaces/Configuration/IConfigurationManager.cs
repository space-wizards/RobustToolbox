namespace ClientInterfaces
{
    public interface IConfigurationManager
    {
        void Initialize(string configFile);
        void SetPlayerName(string name);
        void SetServerAddress(string address);
        string GetPlayerName();
        string GetResourcePath();
        string GetResourcePassword();
        uint GetDisplayWidth();
        uint GetDisplayHeight();
    }
}
