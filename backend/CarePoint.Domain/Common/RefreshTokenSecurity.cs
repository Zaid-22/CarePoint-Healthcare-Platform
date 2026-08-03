using System.Security.Cryptography;
using System.Text;

namespace CarePoint.Domain.Common;

public static class RefreshTokenSecurity
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool IsReuseDetected(bool isAlreadyRevoked, int successfulRevocationCount) =>
        isAlreadyRevoked || successfulRevocationCount != 1;
}
