namespace SS14.Client.Interfaces.Graphics
{
    public interface IDisplayManagerOpenGL
    {
        void Render(FrameEventArgs args);
        void ProcessInput(FrameEventArgs frameEventArgs);
    }
}
