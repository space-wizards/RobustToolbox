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
        public string ResourcePack = @"..\..\..\..\Media\ResourcePack.zip";
        public string ResourcePassword;
        public string ServerAddress = "127.0.0.1";
        public bool SimulateLatency = false;
        public float SimulatedLoss = 0f;
        public float SimulatedMinimumLatency = 0f;
        public float SimulatedRandomLatency = 0f;
        public bool VSync = true;
        public int Rate = 10240; //10 KBytes/s
        public int UpdateRate = 20; //Updates from the server per second
        public int CommandRate = 30; //Commands to the server per second
        public float Interpolation = 0.1f; //Number of seconds behind to render interpolation
        public char ConsoleKey = '#';
    }
}