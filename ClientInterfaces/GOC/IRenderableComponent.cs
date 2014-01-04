using System.Drawing;
using GameObject;
using GorgonLibrary;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IRenderableComponent
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