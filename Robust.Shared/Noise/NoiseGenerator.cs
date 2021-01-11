using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Shared.Noise
{
    [PublicAPI]
    public sealed class NoiseGenerator
    {
        [PublicAPI]
        public enum NoiseType : byte
        {
            Fbm = 0,
            Ridged = 1
        }

        private readonly FastNoise _fastNoiseInstance;

        private float _periodX;
        private float _periodY;

        public NoiseGenerator(NoiseType type)
        {
            _fastNoiseInstance = new FastNoise();
            _fastNoiseInstance.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _fastNoiseInstance.SetFractalLacunarity((float) (Math.PI * 2 / 3));

            switch (type)
            {
                case NoiseType.Fbm:
                    _fastNoiseInstance.SetFractalType(FastNoise.FractalType.FBM);
                    break;
                case NoiseType.Ridged:
                    _fastNoiseInstance.SetFractalType(FastNoise.FractalType.RigidMulti);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void SetFrequency(float frequency)
        {
            _fastNoiseInstance.SetFrequency(frequency);
        }

        public void SetLacunarity(float lacunarity)
        {
            _fastNoiseInstance.SetFractalLacunarity(lacunarity);
        }

        public void SetPersistence(float persistence)
        {
            _fastNoiseInstance.SetFractalGain(persistence);
        }

        public void SetPeriodX(float periodX)
        {
            _periodX = periodX;
        }

        public void SetPeriodY(float periodY)
        {
            _periodY = periodY;
        }

        public void SetOctaves(uint octaves)
        {
            _fastNoiseInstance.SetFractalOctaves((int) octaves);
        }

        public void SetSeed(uint seed)
        {
            _fastNoiseInstance.SetSeed((int) seed);
        }

        public float GetNoiseTiled(float x, float y)
        {
            return GetNoiseTiled((x, y));
        }

        public float GetNoiseTiled(Vector2 vec)
        {
            var s = vec.X / _periodX;
            var t = vec.Y / _periodY;

            const float x1 = 0;
            const float x2 = 1;
            const float y1 = 0;
            const float y2 = 1;

            const float dx = x2 - x1;
            const float dy = y2 - y1;

            const float tau = (float)Math.PI * 2;

            const float dxTau = dx / tau;
            const float dyTau = dy / tau;

            var nx = x1 + (float)Math.Cos(s * tau) * dxTau;
            var ny = y1 + (float)Math.Cos(t * tau) * dyTau;
            var nz = x1 + (float)Math.Sin(s * tau) * dxTau;
            var nw = y1 + (float)Math.Sin(t * tau) * dyTau;

            return GetNoise(nx, ny, nz, nw);
        }

        public float GetNoise(float x)
        {
            return GetNoise((x, 0));
        }

        public float GetNoise(float x, float y)
        {
            return _fastNoiseInstance.GetSimplexFractal(x, y);
        }

        public float GetNoise(Vector2 vector)
        {
            return GetNoise(vector.X, vector.Y);
        }

        public float GetNoise(float x, float y, float z)
        {
            return _fastNoiseInstance.GetSimplexFractal(x, y, z);
        }

        public float GetNoise(Vector3 vector)
        {
            return GetNoise(vector.X, vector.Y, vector.Z);
        }

        public float GetNoise(float x, float y, float z, float w)
        {
            return _fastNoiseInstance.GetSimplex(x, y, z, w);
        }

        public float GetNoise(Vector4 vector)
        {
            return GetNoise(vector.X, vector.Y, vector.Z, vector.W);
        }
    }
}
