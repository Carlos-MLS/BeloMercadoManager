using System.Security.Cryptography;
using BeloMercado.Application.Interfaces.Auth;

namespace BeloMercado.Infrastructure.Auth;

public class RandomPasswordGenerator : IPasswordGenerator
{
    private const int Length = 12;
    private const string Uppercase = "ABCDEFGHIJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*-_=+";
    private static readonly string AllChars = Uppercase + Lowercase + Digits + Symbols;

    public string GenerateTemporaryPassword()
    {
        var chars = new char[Length];

        // Guarantee at least one of each category to satisfy the BCrypt user
        // policy (>= 3 of 4 categories).
        chars[0] = Pick(Uppercase);
        chars[1] = Pick(Lowercase);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Symbols);

        for (var i = 4; i < Length; i++)
        {
            chars[i] = Pick(AllChars);
        }

        Shuffle(chars);
        return new string(chars);
    }

    private static char Pick(string pool)
    {
        var idx = RandomNumberGenerator.GetInt32(pool.Length);
        return pool[idx];
    }

    private static void Shuffle(char[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
