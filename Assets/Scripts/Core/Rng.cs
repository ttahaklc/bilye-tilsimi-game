using System;

namespace Bilye.Core
{
    /// <summary>Small deterministic PRNG (mulberry32), so a level's chain and track can be reproduced in tests.</summary>
    public sealed class Rng
    {
        private uint a;

        public Rng(long seed) { a = unchecked((uint)seed); }

        public static Rng Random() => new Rng(new System.Random().Next());

        /// <summary>Uniform value in [0, 1).</summary>
        public double Next()
        {
            unchecked
            {
                a += 0x6d2b79f5u;
                uint t = a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        public int Int(int n) => (int)Math.Floor(Next() * n);
    }
}
