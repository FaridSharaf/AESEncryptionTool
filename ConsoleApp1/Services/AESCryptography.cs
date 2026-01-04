using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleApp1.Services
{
    public static class AESCryptography
    {
        /// <summary>
        /// Single encryption using plaintext key and IV (original Encrypt method)
        /// </summary>
        private static string EncryptOnce(string plainText, string key, string iv)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using Aes aesAlg = Aes.Create();
            aesAlg.Key = keyBytes;
            aesAlg.IV = ivBytes;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            csEncrypt.Write(plainBytes, 0, plainBytes.Length);
            csEncrypt.FlushFinalBlock();
            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        /// <summary>
        /// Single decryption using plaintext key and IV (original Decrypt method)
        /// </summary>
        private static string DecryptOnce(string cipherText, string key, string iv)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using Aes aesAlg = Aes.Create();
            aesAlg.Key = keyBytes;
            aesAlg.IV = ivBytes;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new MemoryStream(cipherBytes);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }

        /// <summary>
        /// Double encryption - encrypts twice using plaintext key and IV
        /// This is the standard method for phone numbers
        /// </summary>
        public static string Encrypt(string plainText, string key, string iv)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            // First encryption
            string firstEncrypt = EncryptOnce(plainText, key, iv);
            // Second encryption (double encryption)
            string doubleEncrypt = EncryptOnce(firstEncrypt, key, iv);
            
            return doubleEncrypt;
        }

        /// <summary>
        /// Double decryption - decrypts twice using plaintext key and IV
        /// This is the standard method for phone numbers
        /// </summary>
        public static string Decrypt(string cipherText, string key, string iv)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                // First decryption
                string firstDecrypt = DecryptOnce(cipherText, key, iv);
                // Second decryption (double decryption)
                string doubleDecrypt = DecryptOnce(firstDecrypt, key, iv);
                
                return doubleDecrypt;
            }
            catch
            {
                throw new CryptographicException("Decryption failed. Invalid ciphertext or keys.");
            }
        }

        /// <summary>
        /// Single encryption using Base64-encoded key and IV (Encrypt2 method)
        /// This is for ClientId/X-Message encryption
        /// </summary>
        public static string EncryptSingle(string plainText, string keyBase64, string vectorBase64)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using Aes aesAlgorithm = Aes.Create();
            aesAlgorithm.Key = Convert.FromBase64String(keyBase64);
            aesAlgorithm.IV = Convert.FromBase64String(vectorBase64);

            ICryptoTransform encryptor = aesAlgorithm.CreateEncryptor();

            byte[] encryptedData;

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            encryptedData = ms.ToArray();

            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Single decryption using Base64-encoded key and IV (Decrypt2 method)
        /// This is for ClientId/X-Message decryption
        /// </summary>
        public static string DecryptSingle(string cipherText, string keyBase64, string vectorBase64)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using Aes aesAlgorithm = Aes.Create();
                aesAlgorithm.Key = Convert.FromBase64String(keyBase64);
                aesAlgorithm.IV = Convert.FromBase64String(vectorBase64);

                ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();

                using MemoryStream msDecrypt = new MemoryStream(cipherBytes);
                using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new StreamReader(csDecrypt);
                return srDecrypt.ReadToEnd();
            }
            catch
            {
                throw new CryptographicException("Decryption failed. Invalid ciphertext or keys.");
            }
        }

        /// <summary>
        /// Detects if input looks like Base64 encrypted text
        /// </summary>
        public static bool LooksLikeEncrypted(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.Length < 16)
                return false;

            try
            {
                Convert.FromBase64String(input);
                return input.Length >= 20 && (input.Length % 4 == 0);
            }
            catch
            {
                return false;
            }
        }
    }
}
