using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces.GameObjects.Components
{
    public interface IGodotTransformComponent : ITransformComponent
    {
        new IGodotTransformComponent Parent { get; }
        Godot.Node2D SceneNode { get; }
    }
}
