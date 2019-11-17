namespace Robust.Client.Interfaces.UserInterface
{
    public interface IDebugMonitors
    {
        bool Visible { get; set; }
        bool ShowFPS { get; set; }
        bool ShowCoords { get; set; }
        bool ShowNet { get; set; }
        bool ShowTime { get; set; }
        bool ShowFrameGraph { get; set; }
        bool ShowMemory { get; set; }
        bool ShowClyde { get; set; }
        bool ShowInput { get; set; }
    }
}
