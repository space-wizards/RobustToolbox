using SS14.Client.Graphics;
using SS14.Client.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client
{
    public class SceneTreeHolder : ISceneTreeHolder
    {
        public Godot.SceneTree SceneTree { get; private set; }

        public void Initialize(Godot.SceneTree tree)
        {
            SceneTree = tree ?? throw new ArgumentNullException(nameof(tree));

            WorldLayer = new Godot.CanvasLayer()
            {
                Name = "WorldLayer",
                Layer = CanvasLayers.LAYER_WORLD
            };
            WorldRoot = new Godot.Node2D
            {
                Name = "WorldRoot"
            };
            SceneTree.GetRoot().AddChild(WorldLayer);
            WorldLayer.AddChild(WorldRoot);
        }

        private Godot.CanvasLayer WorldLayer;
        public Godot.Node2D WorldRoot { get; private set; }
    }
}
