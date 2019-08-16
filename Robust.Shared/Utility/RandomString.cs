using System;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility
{
    public class RandomString
    {
        private const string _chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Generate(int size)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var buffer = new char[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars[random.Next(_chars.Length)];
            }
            return new string(buffer);
        }
    }
}
