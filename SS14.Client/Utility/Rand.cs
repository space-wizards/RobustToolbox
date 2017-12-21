using SS14.Client.Interfaces.Utility;
using System;

namespace SS14.Client.Utility
{
    public class Rand : IRand
    {
        private readonly Random rand;

        public Rand()
        {
            rand = new Random(DateTime.Now.Millisecond);
        }

        #region IRand Members

        public int Next()
        {
            return rand.Next();
        }

        public int Next(int maxValue)
        {
            return rand.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return rand.Next(minValue, maxValue);
        }

        public void NextBytes(byte[] buffer)
        {
            rand.NextBytes(buffer);
        }

        public double NextDouble()
        {
            return rand.NextDouble();
        }

        #endregion IRand Members
    }
}
