using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    public class DebugSettings
    {
        public bool DebugTextboxes { get; private set; }
        public bool DebugWalls     { get; private set; }
        public bool DebugAtmos     { get; private set; }
        public bool DebugEntities  { get; private set; }
        public bool DebugSprite    { get; private set; }
        public bool DebugColliders { get; private set; }

        public DebugSettings()
        {
            DebugAtmos = false;
            DebugEntities = false;
            DebugTextboxes = false;
            DebugWalls = false;
        }

        public void ToggleAtmosDebug()
        {
            DebugAtmos = true;
        }

        public void ToggleWallDebug()
        {
            DebugWalls = true;
        }

        public void ToggleTextboxDebug()
        {
            DebugTextboxes = true;
        }

        public void ToggleEntitiesDebug()
        {
            DebugEntities= true;
        }

        



    }
}
