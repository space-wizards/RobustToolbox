using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Interfaces.Debugging
{
    public interface IDebugDrawingManager
    {

        void Initialize();
        void Update(float frameTime);
    }
}
