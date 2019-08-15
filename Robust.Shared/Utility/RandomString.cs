using System;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility
{
    public class RandomString
    {
        private const string _chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly IRobustRandom Rand = IoCManager.Resolve<IRobustRandom>();

        public static string Generate(int size)
        {
            var buffer = new char[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars[Rand.Next(_chars.Length)];
            }
            return new string(buffer);
        }
    }
}
