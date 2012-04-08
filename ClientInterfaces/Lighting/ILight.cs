using System.Collections.Generic;
using GorgonLibrary;

namespace ClientInterfaces.Lighting
{
    public interface ILight
    {
        Vector2D Position { get; }
        void Move(Vector2D toPosition);
    }
}
