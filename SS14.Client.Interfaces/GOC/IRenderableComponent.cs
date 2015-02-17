using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Drawing;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GOC
{
    public interface IRenderableComponent : IComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        void Render(Vector2 topLeft, Vector2 bottomRight);
        RectangleF AABB { get; }
        RectangleF AverageAABB { get; }
        bool IsSlaved();
        void SetMaster(Entity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
    }
}