using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerTransformComponent : ITransformComponent
    {
        void DetachParent();
        void AttachParent(ITransformComponent parent);
    }
}
