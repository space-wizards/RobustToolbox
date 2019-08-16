namespace Robust.Shared.Interfaces.Random
{
    public interface IRobustRandom
    {
        int Next();
        int Next(int minValue, int maxValue);
        int Next(int maxValue);
        double NextDouble();
        void NextBytes(byte[] buffer);
    }
}
