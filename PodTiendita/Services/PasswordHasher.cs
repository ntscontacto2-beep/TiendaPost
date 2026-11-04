using System.Security.Cryptography;

namespace PodTiendita.Services
{
    /// <summary>PBKDF2 con sal aleatoria. Formato guardado: iteraciones.sal.hash (Base64).</summary>
    public static class PasswordHasher
    {
        private const int Iteraciones = 100_000;
        private const int TamSal = 16;
        private const int TamHash = 32;

        public static string Hash(string password)
        {
            var sal = RandomNumberGenerator.GetBytes(TamSal);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamHash);
            return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verificar(string password, string almacenado)
        {
            var partes = almacenado.Split('.');
            if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones)) return false;

            var sal = Convert.FromBase64String(partes[1]);
            var esperado = Convert.FromBase64String(partes[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(actual, esperado);
        }
    }
}
