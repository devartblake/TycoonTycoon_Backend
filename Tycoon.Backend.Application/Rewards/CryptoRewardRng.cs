using System.Security.Cryptography;

namespace Tycoon.Backend.Application.Rewards;

public sealed class CryptoRewardRng : IRewardRng
{
    public double NextDouble()
    {
        // Generate 8 random bytes, mask to 53-bit mantissa for uniform [0,1)
        Span<byte> buf = stackalloc byte[8];
        RandomNumberGenerator.Fill(buf);
        var bits = BitConverter.ToUInt64(buf) >> 11;
        return bits / (double)(1UL << 53);
    }
}
