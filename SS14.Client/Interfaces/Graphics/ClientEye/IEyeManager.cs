namespace SS14.Client.Interfaces.Graphics.ClientEye
{
    public interface IEyeManager
    {
        IEye CurrentEye { get; set; }
        void Initialize();
    }
}
