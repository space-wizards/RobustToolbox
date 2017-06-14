using SS14.Shared.GameObjects;
using System;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISVarsComponent
    {
        event EventHandler<GetSVarsEventArgs> GetSVarsCallback;
        void DoSetSVar(MarshalComponentParameter svar);
        void DoGetSVars();
    }
}
