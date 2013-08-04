using System;

namespace ClientServices.Configuration
{
    [Serializable]
    public class Configuration
    {
        public uint DisplayHeight = 768;
        public uint DisplayRefresh = 60;
        public uint DisplayWidth = 1024;
        public bool Fullscreen;
        public bool MessageLogging = false;
        public string PlayerName = "Joe Genero";
        public string ResourcePack = @"..\..\..\Media\ResourcePack.zip";
        public string ResourcePassword;
        public string ServerAddress = "127.0.0.1";
        public bool SimulateLatency = false;
        public float SimulatedLoss = 0f;
        public float SimulatedMinimumLatency = 0f;
        public float SimulatedRandomLatency = 0f;
        public bool VSync = true;
    }
}