namespace Robust.Server.Console
{
    [NotContentImplementable]
    public interface IConGroupController : IConGroupControllerImplementation
    {
        public IConGroupControllerImplementation Implementation { set; }
    }
}
