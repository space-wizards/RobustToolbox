using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Client.Services.Lighting
{
    public class ShadowmapSize
    {
        private int Radius;
        


        public ShadowmapSize ( )
        {

        }


        public static ShadowmapSize Size128
        {
            get;
            set;
        }
        public static ShadowmapSize Size256
        {
            get;
            set;
        }
        public static ShadowmapSize Size512
        {
            get;
            set;
        }
        public static ShadowmapSize Size1024
        {
            get;
            set;
        }
        public static ShadowmapSize Size2048
        {
            get;
            set;
        }
    }
}
