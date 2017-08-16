using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        void Render(Vector2 topLeft, Vector2 bottomRight);
        FloatRect AABB { get; }
        FloatRect AverageAABB { get; }
        bool IsSlaved();
        void SetMaster(IEntity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
    }
}
