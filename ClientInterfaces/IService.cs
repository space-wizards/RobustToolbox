using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;


namespace ClientInterfaces
{
    public interface IService
    {
        ClientServiceType ServiceType { get; }
    }
}
