using System.Collections.Generic;
using GorgonLibrary;

namespace ClientInterfaces.Lighting
{
    public interface ILight
    {
        int Radius {get;}
        Vector2D Position { get; }
        void Move(Vector2D toPosition);
        void SetRadius(int Radius);
    }
}
