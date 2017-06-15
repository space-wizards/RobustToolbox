using System;

namespace SS14.Shared.Utility
{
    public class RandomString
    {
        private const string _chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static Random _rand;

        public static string Generate(int size)
        {
            if (_rand == null)
                _rand = new Random();

            var buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars[_rand.Next(_chars.Length)];
            }
            return new string(buffer);
        }
    }
}