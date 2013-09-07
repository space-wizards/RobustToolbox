using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.GameTimer;

namespace ClientServices.GameTimer
{
    public class GameTimer : IGameTimer
    {
        public GameTimer()
        {
            CurrentTime = 0f;
        }
        public float CurrentTime { get; private set; }
        public void UpdateTime(float delta)
        {
            CurrentTime += delta;
        }
        public void SetTime(float time)
        {
            CurrentTime = time;
        }
    }
}
