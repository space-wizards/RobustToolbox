using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public interface IGameObjectComponent
    {
        Entity Owner { get; set; }

        void RecieveMessage(MessageType type, params object[] list);
        void OnRemove();
        void OnAdd(Entity owner);
        void Update(float frameTime);
    }
}
