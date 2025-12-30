using System.Security.Cryptography;

namespace Tycoon.Backend.Application.Referrals
{
    public static class ReferralCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no confusing chars
        public static string Generate(int length = 8)
        {
            Span<byte> bytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(bytes);

            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = Alphabet[bytes[i] % Alphabet.Length];

            return new string(chars);
        }
    }
}
