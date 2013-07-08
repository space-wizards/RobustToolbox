using System;

namespace ClientServices.Configuration
{
    [Serializable]
    public class Configuration
    {
        public uint DisplayWidth = 1024;
        public uint DisplayHeight = 768;
        public uint DisplayRefresh = 60;
        public bool Fullscreen;
        public bool VSync = true;
        public string ResourcePack = @"..\..\..\Media\ResourcePack.zip";
        public string ResourcePassword;
        public string PlayerName = "Joe Genero";
        public string ServerAddress = "127.0.0.1";
        public bool MessageLogging = false;
        public bool SimulateLatency = false;
        public float SimulatedLoss = 0f;
        public float SimulatedMinimumLatency = 0f;
        public float SimulatedRandomLatency = 0f;
    }
}
