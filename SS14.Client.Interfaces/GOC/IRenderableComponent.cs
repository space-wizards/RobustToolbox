using GorgonLibrary;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Drawing;

namespace SS14.Client.Interfaces.GOC
{
    public interface IRenderableComponent : IComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        void Render(Vector2D topLeft, Vector2D bottomRight);
        RectangleF AABB { get; }
        RectangleF AverageAABB { get; }
        bool IsSlaved();
        void SetMaster(Entity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
    }
}