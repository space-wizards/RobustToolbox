using SS14.Client.Interfaces;
using System;

namespace SS14.Client
{
    public class SceneTreeHolder : ISceneTreeHolder
    {
        public Godot.SceneTree SceneTree { get; private set; }
        public Godot.Node2D WorldRoot { get; private set; }

        public void Initialize(Godot.SceneTree tree)
        {
            SceneTree = tree ?? throw new ArgumentNullException(nameof(tree));

            WorldRoot = new Godot.Node2D
            {
                Name = "WorldRoot"
            };
            SceneTree.GetRoot().AddChild(WorldRoot);
        }
    }
}
