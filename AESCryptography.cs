using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AngelSQLServer
{
    public class AESCryptography
    {
        private static readonly int KeySize = 256; // Tamaño de la clave en bits (AES-256)

        public static string EncryptString(string plainText, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.GenerateIV(); // Genera un IV único para esta encriptación
                byte[] iv = aes.IV;

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length); // Escribe el IV al inicio

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string DecryptString(string cipherText, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;

                // Extraer el IV (los primeros 16 bytes)
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // Extraer los datos cifrados
                byte[] encryptedData = new byte[cipherBytes.Length - iv.Length];
                Array.Copy(cipherBytes, iv.Length, encryptedData, 0, encryptedData.Length);

                using (MemoryStream ms = new MemoryStream(encryptedData))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd(); // Retorna el texto desencriptado
                        }
                    }
                }
            }
        }

        public static string GenerateKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] keyBytes = new byte[KeySize / 8]; // 256 bits -> 32 bytes
                rng.GetBytes(keyBytes);
                // Convert the random bytes to Base64 so the key can be reliably
                // stored and later converted back to the original bytes.
                return Convert.ToBase64String(keyBytes);
            }
        }
    }
}
