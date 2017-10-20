using SS14.Client.Graphics.Render;

namespace SS14.Client.Graphics
{
    public class Debug
    {
        public bool DebugTextboxes { get; private set; }
        public bool DebugWalls     { get; private set; }
        public bool DebugAtmos     { get; private set; }
        public bool DebugEntities  { get; private set; }
        public bool DebugSprite    { get; private set; }
        public bool DebugColliders { get; private set; }
        public bool DebugGridDisplay { get; private set; }
        public bool DebugFPS { get; private set; }

        public Debug()
        {
            DebugAtmos = false;
            DebugEntities = false;
            DebugTextboxes = false;
            DebugWalls = false;
            DebugGridDisplay = false;
        }

        public void ToggleGridDisplayDebug()
        {
            DebugGridDisplay = !DebugGridDisplay;
        }

        public void ToggleAtmosDebug()
        {
            DebugAtmos = !DebugAtmos;
        }

        public void ToggleWallDebug()
        {
            DebugWalls = !DebugWalls;
        }

        public void ToggleTextboxDebug()
        {
            DebugTextboxes = !DebugTextboxes;
        }

        public void ToggleEntitiesDebug()
        {
            DebugEntities = !DebugEntities;
        }

        public void ToggleAABBDebug()
        {
            DebugColliders = !DebugColliders;
        }

        public void ToggleFPSDebug()
        {
            DebugFPS = !DebugFPS;
        }
    }
}
