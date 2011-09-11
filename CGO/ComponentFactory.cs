using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    class ComponentFactory
    {
        private static ComponentFactory singleton;
        public static ComponentFactory Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new ComponentFactory();
                return singleton;
            }
            private set { }
        }

        public ComponentFactory()
        {

        }
    }
}
