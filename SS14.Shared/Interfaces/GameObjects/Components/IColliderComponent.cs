using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    public interface IColliderComponent : IComponent
    {
        FloatRect WorldAABB { get; }
    }
}
