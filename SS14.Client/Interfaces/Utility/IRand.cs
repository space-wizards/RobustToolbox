using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.Utility
{
    public interface IRand : IIoCInterface
    {
        int Next();
        int Next(int maxValue);
        int Next(int minValue, int maxValue);
        void NextBytes(byte[] buffer);
        double NextDouble();
    }
}
