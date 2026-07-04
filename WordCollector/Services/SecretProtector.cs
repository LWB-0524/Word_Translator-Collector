using System.Security.Cryptography;
using System.Text;

namespace WordCollector.Services;

internal static class SecretProtector
{
    private const string ProtectedPrefix = "dpapi:";
    private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("WordCollector.ApiKey.v1");

    public static string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext),
            OptionalEntropy,
            DataProtectionScope.CurrentUser);
        return ProtectedPrefix + Convert.ToBase64String(encrypted);
    }

    public static bool TryUnprotect(string? storedValue, out string plaintext, out bool wasProtected)
    {
        plaintext = string.Empty;
        wasProtected = false;

        if (string.IsNullOrEmpty(storedValue))
            return true;

        if (!storedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
        {
            plaintext = storedValue;
            return true;
        }

        wasProtected = true;
        try
        {
            var payload = Convert.FromBase64String(storedValue[ProtectedPrefix.Length..]);
            var decrypted = ProtectedData.Unprotect(
                payload,
                OptionalEntropy,
                DataProtectionScope.CurrentUser);
            plaintext = Encoding.UTF8.GetString(decrypted);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
