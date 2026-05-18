using System.Security.Cryptography;

namespace SwiftPos.Api.Security;

public sealed class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 100_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Join(
            ':',
            Algorithm,
            DefaultIterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var segments = passwordHash.Split(':');
        if (segments.Length != 4 || segments[0] != Algorithm || !int.TryParse(segments[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(segments[2]);
            var expectedHash = Convert.FromBase64String(segments[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
