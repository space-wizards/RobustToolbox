using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces
{
    public interface ISceneTreeHolder
    {
        void Initialize(Godot.SceneTree tree);

        Godot.SceneTree SceneTree { get; }
        Godot.Node2D WorldRoot { get; }
    }
}
