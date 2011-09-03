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
        public AlignmentOptions AlignOption = AlignmentOptions.AlignTile; //Alignment type. See enum declaration for infos.
        public bool placeAnywhere = false;  //Can be placed anywhere without limitations. Overrides other options. Tiles will still be placed in grid. Derp.
    }
}
