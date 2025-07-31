using System.Security.Cryptography;

namespace SadieTester;

public static class SecureRandom
{
    private static int Next(int minValue, int maxValue)
    {
        return RandomNumberGenerator.GetInt32(minValue, maxValue);
    }

    public static bool OneIn(int chance)
    {
        if (chance <= 1) return true;
        return Next(1, chance + 1) == 1;
    }
}