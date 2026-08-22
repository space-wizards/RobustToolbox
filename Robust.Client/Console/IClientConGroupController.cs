namespace Robust.Client.Console
{
    [NotContentImplementable]
    public interface IClientConGroupController : IClientConGroupImplementation
    {
        IClientConGroupImplementation Implementation { set; }
    }
}
