using SS14.Client.Interfaces.GameTimer;

namespace SS14.Client.Services.GameTimer
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
