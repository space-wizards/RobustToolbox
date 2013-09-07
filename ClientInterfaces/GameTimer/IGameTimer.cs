using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientInterfaces.GameTimer
{
    public interface IGameTimer
    {
        float CurrentTime { get; }
        void UpdateTime(float delta);
        void SetTime(float time);
    }
}
