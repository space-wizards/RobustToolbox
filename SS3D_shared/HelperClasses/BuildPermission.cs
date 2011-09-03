using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.HelperClasses
{
    public class BuildPermission
    {
        public ushort mobUid;               //UID of mob this permission is for.
        public ushort range = 0;            //Valid range from mob.
        public string type = "";            //Object name / type.
        public bool attachesToWall = false; //Can only be placed on 'solid' tiles.
        public bool snapToSimilar = false;  //Will snap to similar nearby objects. (For windows, tables etc.). THIS DOES NOT WORK WITH TILES. USE SNAPTOGRID FOR THOSE.
        public bool snapToTiles = false;    //Will snap to tiles.
        public bool placeAnywhere = false;  //Can be placed anywhere without limitations. Overrides other options.
    }
}
