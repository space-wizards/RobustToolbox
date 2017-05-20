using SS14.Shared.GO;
using System;

namespace SS14.Client.Interfaces.GOC
{
    public interface ISVarsComponent
    {
        event EventHandler<GetSVarsEventArgs> GetSVarsCallback;
        void DoSetSVar(MarshalComponentParameter svar);
        void DoGetSVars();
    }
}