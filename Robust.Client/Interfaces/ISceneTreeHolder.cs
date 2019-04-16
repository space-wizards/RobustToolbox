using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Interfaces
{
    internal interface ISceneTreeHolder
    {
        void Initialize(Godot.SceneTree tree);

        Godot.CanvasLayer BelowWorldScreenSpace { get; }
        Godot.SceneTree SceneTree { get; }
        Godot.Node2D WorldRoot { get; }
    }
}
