using System;
using System.Collections.Generic;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
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