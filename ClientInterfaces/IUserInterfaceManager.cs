using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientInterfaces
{
    public interface IUserInterfaceManager : IService
    {
        void ComponentUpdate(GuiComponentType type, params object[] args);
    }
}
