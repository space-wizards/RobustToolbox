using SS14.Shared.GO;
using System;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GOC
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