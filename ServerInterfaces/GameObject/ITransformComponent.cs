using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ServerInterfaces.GameObject
{
    public interface ITransformComponent
    {
        Vector2 Position { get; set; }
        void TranslateTo(Vector2 toPosition);
        void TranslateByOffset(Vector2 offset);
    }
}
