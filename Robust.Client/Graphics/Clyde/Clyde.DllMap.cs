using System;
using System.Runtime.InteropServices;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        static Clyde()
        {
            if (OperatingSystem.IsWindows() &&
                RuntimeInformation.ProcessArchitecture == Architecture.X64 &&
                Environment.GetEnvironmentVariable("ROBUST_INTEGRATED_GPU") != "1")
            {
                // We force load nvapi64.dll so nvidia gives us the dedicated GPU on optimus laptops.
                // This is 100x easier than nvidia's documented approach of NvOptimusEnablement,
                // and works while developing.
                NativeLibrary.TryLoad("nvapi64.dll", out _);
                // If this fails whatever.
            }
        }
    }
}
