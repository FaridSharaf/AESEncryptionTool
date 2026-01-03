
using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;

//var localTime = new DateTime(2022, 1, 13, 16, 25, 35, 125, DateTimeKind.Local);
//var utcTime = new DateTime(2022, 1, 13, 16, 25, 35, 125, DateTimeKind.Utc);

//Console.WriteLine(utcTime);

public class AESCryptography
{
    public static string Encrypt(string plainText, string key, string iv)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = keyBytes;
            aesAlg.IV = ivBytes;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                    csEncrypt.FlushFinalBlock();
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }

    public static string Decrypt(string cipherText, string key, string iv)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = keyBytes;
            aesAlg.IV = ivBytes;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
    public static string Encrypt2(string plainText, string keyBase64, string vectorBase64)
    {
        using Aes aesAlgorithm = Aes.Create();
        //set the parameters with out keyword
        aesAlgorithm.Key = Convert.FromBase64String(keyBase64);
        aesAlgorithm.IV = Convert.FromBase64String(vectorBase64);

        // Create encryptor object
        ICryptoTransform encryptor = aesAlgorithm.CreateEncryptor();

        byte[] encryptedData;

        //Encryption will be done in a memory stream through a CryptoStream object
        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (StreamWriter sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        encryptedData = ms.ToArray();

        return Convert.ToBase64String(encryptedData);
    }
    public static void Main()
    {
        string key = "KEY";
        string iv = "IV";
        string plainText = "01098777895"; 

        // Double Encryption
        string encryptedText = Encrypt(plainText, key, iv);
        Console.WriteLine(  "firwst encrypt : " + encryptedText);
        string doubleEncryptedText = Encrypt(encryptedText, key, iv);
        Console.WriteLine("Double Encrypted Text: " + doubleEncryptedText);

        // Double Decryption
        string decryptedText = Decrypt(doubleEncryptedText, key, iv);
        string doubleDecryptedText = Decrypt(decryptedText, key, iv);
        Console.WriteLine("Double Decrypted Text: " + doubleDecryptedText);
    }
}

