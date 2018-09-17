namespace SS14.Client.Interfaces.UserInterface
{
    public interface IDebugMonitors
    {
        bool Visible { get; set; }
        bool ShowFPS { get; set; }
        bool ShowCoords { get; set; }
        bool ShowNet { get; set; }
        bool ShowTime { get; set; }
    }
}
