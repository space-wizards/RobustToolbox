namespace SGO.Component.Think.ThinkComponent
{
    public interface IThinkComponent
    {
        void OnBump(object sender, params object[] list);
        void Update(float frameTime);
    }
}