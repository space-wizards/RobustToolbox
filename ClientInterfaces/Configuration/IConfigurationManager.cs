namespace ClientInterfaces.Configuration
{
    public interface IConfigurationManager
    {
        void Initialize(string configFile);
        void SetPlayerName(string name);
        void SetServerAddress(string address);
        void SetFullscreen(bool fullscreen);
        void SetResolution(uint width, uint height);
        string GetPlayerName();
        string GetResourcePath();
        string GetResourcePassword();
        bool GetFullscreen();
        uint GetDisplayWidth();
        uint GetDisplayHeight();
        string GetServerAddress();
        bool GetMessageLogging();
        bool GetSimulateLatency();
        float GetSimulatedLoss();
        float GetSimulatedMinimumLatency();
        float GetSimulatedRandomLatency();
    }
}
