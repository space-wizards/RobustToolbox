using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GameObjects
{
    public class GetSVarsEventArgs : EventArgs
    {
        public List<MarshalComponentParameter> SVars;

        public GetSVarsEventArgs(List<MarshalComponentParameter> sVars)
        {
            SVars = sVars;
        }
    }
}
