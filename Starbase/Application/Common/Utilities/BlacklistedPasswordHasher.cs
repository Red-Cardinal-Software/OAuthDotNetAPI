using System.Text;

namespace Application.Common.Utilities;

public static class BlacklistedPasswordHasher
{
    public static string GenerateHashedPasswordStringForBlacklistCheck(string password)
    {
        var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);

        var passwordBytes = Encoding.UTF8.GetBytes(password);

        hashAlgorithm.BlockUpdate(passwordBytes, 0, passwordBytes.Length);

        var hashOutput = new byte[64];
        hashAlgorithm.DoFinal(hashOutput, 0);

        return BitConverter.ToString(hashOutput);
    }
}
