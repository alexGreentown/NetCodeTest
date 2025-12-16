using System.Security.Cryptography;
using System.Text;

namespace NetCodeTest.Lobby
{
    public static class HashUtils
    {
        public static string Sha256Hex(string input)
        {
            input ??= "";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}