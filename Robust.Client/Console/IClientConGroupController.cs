using System;

namespace Robust.Client.Console
{
    public interface IClientConGroupController : IClientConGroupImplementation
    {
        IClientConGroupImplementation Implementation { set; }
    }
}
