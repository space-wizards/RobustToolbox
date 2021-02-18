namespace Robust.Server.Console
{
    public interface IConGroupController : IConGroupControllerImplementation
    {
        public IConGroupControllerImplementation Implementation { set; }
    }
}
