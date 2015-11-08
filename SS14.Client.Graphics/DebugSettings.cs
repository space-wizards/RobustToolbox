using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    public class DebugSettings
    {
        public static bool DebugTextboxes { get; private set; }
        public static bool DebugWalls     { get; private set; }
        public static bool DebugAtmos     { get; private set; }
        public static bool DebugEntities  { get; private set; }

        public DebugSettings()
        {
            DebugAtmos = false;
            DebugEntities = false;
            DebugTextboxes = false;
            DebugWalls = false;
        }

        public static void ToggleAtmosDebug()
        {
            DebugAtmos = true;
        }

        public static void ToggleWallDebug()
        {
            DebugWalls = true;
        }

        public static void ToggleTextboxDebug()
        {
            DebugTextboxes = true;
        }

        public static void ToggleEntitiesDebug()
        {
            DebugEntities= true;
        }

        



    }
}
