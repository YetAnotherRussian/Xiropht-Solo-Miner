using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Xiropht_Solo_Miner.Algo
{

    public class ClassAlgoMining
    {
        [ThreadStatic]
        public static ICryptoTransform CryptoTransformMining;

        [ThreadStatic]
        public static AesManaged AesManagedMining;

        [ThreadStatic]
        public static SHA512Managed Sha512ManagedMining;


        public static MemoryStream[] MemoryStreamMining;

        public static CryptoStream[] CryptoStreamMining;

        public static string EncryptAesShare(string text, int idThread)
        {
            if (MemoryStreamMining[idThread] == null)
            {
                MemoryStreamMining[idThread] = new MemoryStream();
            }


            if (CryptoStreamMining[idThread] == null)
            {
                CryptoStreamMining[idThread] =
                    new CryptoStream(MemoryStreamMining[idThread], CryptoTransformMining, CryptoStreamMode.Write);
            }

            var textBytes = Encoding.UTF8.GetBytes(text);
            CryptoStreamMining[idThread].Write(textBytes, 0, textBytes.Length);
            if (!CryptoStreamMining[idThread].HasFlushedFinalBlock)
            {
                CryptoStreamMining[idThread].FlushFinalBlock();
                Array.Clear(textBytes, 0, textBytes.Length);
            }

            string result = BitConverter.ToString(MemoryStreamMining[idThread].ToArray());
            MemoryStreamMining[idThread].SetLength(0);
            CryptoStreamMining[idThread] = new CryptoStream(MemoryStreamMining[idThread], CryptoTransformMining, CryptoStreamMode.Write);
            return result;

        }




        public static byte[] EncryptAesShareByte(byte[] text)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, CryptoTransformMining, CryptoStreamMode.Write))
                {
                    cs.Write(text, 0, text.Length);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Encrypt share with XOR.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string EncryptXorShare(string text, string key)
        {
            var result = new StringBuilder();

            for (int c = 0; c < text.Length; c++)
                result.Append((char) ((uint) text[c] ^ (uint) key[c % key.Length]));
            return result.ToString();
        }




        public static byte[] HashHexToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }


        /// <summary>
        /// Generate a sha512 hash
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GenerateSha512FromByteArray(byte[] bytes)
        {
            if (Sha512ManagedMining == null)
            {
                Sha512ManagedMining = new SHA512Managed();
            }

            var hashedInputBytes = Sha512ManagedMining.ComputeHash(bytes);

            var hashedInputStringBuilder = new StringBuilder(128);
            foreach (var b in hashedInputBytes)
                hashedInputStringBuilder.Append(b.ToString("X2"));
            return hashedInputStringBuilder.ToString();
        }


        /// <summary>
        /// Generate a sha512 hash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GenerateSha512FromString(string input)
        {
            if (Sha512ManagedMining == null)
            {
                Sha512ManagedMining = new SHA512Managed();
            }

            var bytes = Encoding.UTF8.GetBytes(input);

            var hashedInputBytes = Sha512ManagedMining.ComputeHash(bytes);

            var hashedInputStringBuilder = new StringBuilder(128);
            foreach (var b in hashedInputBytes)
                hashedInputStringBuilder.Append(b.ToString("X2"));
            return hashedInputStringBuilder.ToString();

        }
    }
}