using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using SS14.Client.ViewVariables.Instances;

namespace SS14.Client.ViewVariables
{
    internal abstract class ViewVariablesTrait
    {
        protected ViewVariablesInstanceObject Instance { get; private set; }

        public virtual void Initialize(ViewVariablesInstanceObject instance)
        {
            Instance = instance;
        }

        public virtual void Refresh()
        {
        }
    }
}
