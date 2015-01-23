namespace SS14.Server.GameObjects.Think.ThinkComponent
{
    public interface IThinkComponent
    {
        void OnBump(object sender, params object[] list);
        void Update(float frameTime);
    }
}