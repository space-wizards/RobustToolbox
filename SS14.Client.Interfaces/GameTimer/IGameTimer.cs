
namespace SS14.Client.Interfaces.GameTimer
{
    public interface IGameTimer
    {
        float CurrentTime { get; }
        void UpdateTime(float delta);
        void SetTime(float time);
    }
}
