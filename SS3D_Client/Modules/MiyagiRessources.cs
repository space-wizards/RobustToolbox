using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Miyagi;
using Miyagi.Common;
using Miyagi.Common.Resources;

namespace SS3D.Modules
{
    public sealed class MiyagiResources
    {
        static MiyagiResources()
        {
        }

        MiyagiResources()
        {
        }

        public static MiyagiResources Singleton
        {
            get
            {
                return singleton;
            }
        }

        static readonly MiyagiResources singleton = new MiyagiResources();

        public Dictionary<string, Skin> Skins;

        public Dictionary<string, Font> Fonts;

        public MiyagiSystem mMiyagiSystem
        {
            get;
            private set;
        }

        public void Initialize(MiyagiSystem mSystem)
        {
            mMiyagiSystem = mSystem;
        }
    }
}
