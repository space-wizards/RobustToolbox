namespace Robust.Shared.Console
{
    public interface IConGroupController : IConGroupControllerImplementation
    {
        public IConGroupControllerImplementation Implementation { set; }
    }
}
