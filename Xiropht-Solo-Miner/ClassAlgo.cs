using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xiropht_Solo_Miner
{
    public class ClassAlgoErrorEnumeration
    {
        public const string AlgoError = "WRONG";
    }

    public class ClassAlgoEnumeration
    {
        public const string Xor = "XOR"; // 0
        public const string Aes = "AES"; // 1

    }

    public class ClassAlgo
    {

        /// <summary>
        /// Encrypt the result received and retrieve it.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetEncryptedResult(string idAlgo, string result, string key, int size, byte[] keyByte)
        {
            try
            {
                switch (idAlgo)
                {
                    case ClassAlgoEnumeration.Xor:
                        return Xor.EncryptString(result, key);

                    case ClassAlgoEnumeration.Aes:
                        return AesCrypt.EncryptString(result, key, keyByte, size);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("Mining algorithm error: " + error.Message);
            }
            return "WRONG";
        }

        /// <summary>
        /// Return an algo name from id.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <returns></returns>
        public static string GetNameAlgoFromId(int idAlgo)
        {
            switch (idAlgo)
            {
                case 0:
                    return ClassAlgoEnumeration.Xor;
                case 1:
                    return ClassAlgoEnumeration.Aes;
            }

            return "NONE";
        }
    }

    public static class AesCrypt
    {
        public static string EncryptString(string text, string keyCrypt, byte[] keyByte, int size)
        {
            var textByte = Encoding.UTF8.GetBytes(text);
            using (var pdb = new PasswordDeriveBytes(keyCrypt, keyByte))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                    {
                        aes.BlockSize = size;
                        aes.KeySize = size;
                        aes.Key = pdb.GetBytes(aes.KeySize / 8);
                        aes.IV = pdb.GetBytes(aes.BlockSize / 8);
                        using (CryptoStream cs = new CryptoStream(ms,
                          aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(textByte, 0, textByte.Length);
                        }
                        return BitConverter.ToString(ms.ToArray());
                    }
                }
            }
        }
    }

    public static class Xor
    {
        public static string EncryptString(string text, string key)
        {
            var result = new StringBuilder();

            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));
            return result.ToString();
        }
    }
}