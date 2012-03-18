using System;

namespace ClientServices.Configuration
{
    [Serializable]
    public class Configuration
    {
        public uint DisplayWidth = 1024;
        public uint DisplayHeight = 768;
        public bool Fullscreen;
        public bool VSync = true;
        public string ResourcePack = @"..\..\..\Media\ResourcePack.zip";
        public string ResourcePassword;
        public string PlayerName = "Joe Genero";
        public string ServerAddress = "127.0.0.1";
        public bool MessageLogging = false;
    }
}
