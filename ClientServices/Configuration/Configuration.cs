using System;

namespace ClientServices.Configuration
{
    [Serializable]
    public class Configuration
    {
        const int _Version = 2;
        public uint DisplayWidth = 1024;
        public uint DisplayHeight = 768;
        public bool Fullscreen = false;
        public bool VSync = true;
        public string ResourcePack = @"..\..\..\Media\Media.gorPack";
        public string GuiFolder = @"..\..\..\Media\";
        public string PlayerName = "Joe Genero";
    }
}
