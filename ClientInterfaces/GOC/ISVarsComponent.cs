using System;
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