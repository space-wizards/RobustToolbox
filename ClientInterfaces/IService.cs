using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;


namespace ClientInterfaces
{
    public interface IService
    {
        ClientServiceType ServiceType { get; }
    }
}
