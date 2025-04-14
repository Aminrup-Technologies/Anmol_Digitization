using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AnmolDristi
{
    public class AesEncryptionHelper
    {
        private static readonly string EncryptionKey = "Your$uper$ecureKey123!"; // Replace with strong key (save in config)

        public static byte[] Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = GetKeyBytes(EncryptionKey); // Convert key string to 32 bytes (256-bit)
                aesAlg.GenerateIV(); // generate new IV

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aesAlg.IV, 0, aesAlg.IV.Length); // prepend IV

                    using (CryptoStream cs = new CryptoStream(ms, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    return ms.ToArray(); // encrypted data + IV
                }
            }
        }


        public static string Decrypt(byte[] cipherData)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = GetKeyBytes(EncryptionKey);

                // Extract IV (first 16 bytes)
                byte[] iv = new byte[aesAlg.BlockSize / 8]; // AES block size = 128 bits = 16 bytes
                Array.Copy(cipherData, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;

                // Extract the actual encrypted content (after the IV)
                byte[] encryptedData = new byte[cipherData.Length - iv.Length];
                Array.Copy(cipherData, iv.Length, encryptedData, 0, encryptedData.Length);

                using (MemoryStream ms = new MemoryStream(encryptedData))
                using (CryptoStream cs = new CryptoStream(ms, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd(); // decrypted plain text
                }
            }
        }

        private static byte[] GetKeyBytes(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(key)); // 32-byte key
            }
        }

    }
}