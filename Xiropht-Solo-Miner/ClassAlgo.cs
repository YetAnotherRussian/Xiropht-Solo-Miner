using System;
using System.Security.Cryptography;
using System.Text;

namespace Xiropht_Solo_Miner
{

    public class ClassAlgo
    {
        public static string EncryptAesShare(string text, byte[] aesKeyBytes, byte[] aesIvBytes, int size)
        {
            using (var aes = new AesManaged())
            {
                aes.BlockSize = size;
                aes.KeySize = size;
                aes.Key = aesKeyBytes;
                aes.IV = aesIvBytes;

                var encryptor = aes.CreateEncryptor();

                var textBytes = Encoding.UTF8.GetBytes(text);
                var result = encryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);

                return BitConverter.ToString(result);
            }
        }

        public static string EncryptXorShare(string text, string key)
        {
            var result = new StringBuilder();

            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));
            return result.ToString();
        }

    }
}