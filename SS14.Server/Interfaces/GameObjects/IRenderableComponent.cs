using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        bool IsSlaved();
        void SetMaster(IEntity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
        bool Visible { get; set; }
    }
}
