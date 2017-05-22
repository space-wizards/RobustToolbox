using SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.GOC
{
    public interface IRenderableComponent : IComponent
    {
        bool IsSlaved();
        void SetMaster(Entity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
        bool Visible { get; set; }
    }
}
