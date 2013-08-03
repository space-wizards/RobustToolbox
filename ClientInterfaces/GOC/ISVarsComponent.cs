using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface ISVarsComponent
    {
        event EventHandler<GetSVarsEventArgs> GetSVarsCallback;
        void DoSetSVar(MarshalComponentParameter svar);
        void DoGetSVars();
    }
}
