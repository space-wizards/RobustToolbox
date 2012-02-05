namespace ClientInterfaces
{
    public interface IServiceManager
    {
        void Register<T>() where T : IService;
        void Unregister<T>() where T : IService;
        void Update();
        void Render();
        T GetService<T>();
    }
}
