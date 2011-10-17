using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS3D_shared;

namespace ClientInterfaces
{
    public interface IUserInterfaceManager : IService
    {
        void ComponentUpdate(GuiComponentType type, params object[] args);
    }
}
