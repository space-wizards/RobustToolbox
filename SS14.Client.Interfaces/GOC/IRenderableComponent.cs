using SFML.Graphics;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;

namespace SS14.Client.Interfaces.GOC
{
    public interface IRenderableComponent : IComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        void Render(Vector2f topLeft, Vector2f bottomRight);
        FloatRect AABB { get; }
        FloatRect AverageAABB { get; }
        bool IsSlaved();
        void SetMaster(Entity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
    }
}