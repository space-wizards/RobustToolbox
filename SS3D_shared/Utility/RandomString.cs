using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.Utility
{
    public class RandomString
    {
        private static Random _rand;
        private const string _chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Generate(int size)
        {
            if(_rand == null)
                _rand = new Random();

            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = _chars[_rand.Next(_chars.Length)];
            }
            return new string(buffer);
        }
    }
}
