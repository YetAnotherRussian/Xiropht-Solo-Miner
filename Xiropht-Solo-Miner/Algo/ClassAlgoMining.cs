using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Xiropht_Solo_Miner.Algo
{

    public class ClassAlgoMining
    {
        public static ICryptoTransform[] CryptoTransformMining;

        public static AesManaged[] AesManagedMining;

        public static SHA512Managed[] Sha512ManagedMining;

        public static MemoryStream[] MemoryStreamMining;

        public static CryptoStream[] CryptoStreamMining;

        private static readonly char[] HexArrayList = "0123456789ABCDEF".ToCharArray();


        public static string EncryptAesShare(string text, int idThread)
        {
            if (MemoryStreamMining[idThread] == null)
            {
                MemoryStreamMining[idThread] = new MemoryStream();
            }

            if (CryptoStreamMining[idThread] == null)
            {
                CryptoStreamMining[idThread] = new CryptoStream(MemoryStreamMining[idThread],
                    CryptoTransformMining[idThread], CryptoStreamMode.Write);
            }

            #region Do mining work

            var textBytes = Encoding.UTF8.GetBytes(text);
            CryptoStreamMining[idThread].Write(textBytes, 0, textBytes.Length);

            #endregion

            #region Flush mining work process

            if (!CryptoStreamMining[idThread].HasFlushedFinalBlock)
            {
                CryptoStreamMining[idThread].FlushFinalBlock();
                CryptoStreamMining[idThread].Flush();
            }
 

            #endregion

            #region Translate Mining work

            byte[] resultByteShare = MemoryStreamMining[idThread].ToArray();
            string result = GetHexStringFromByteArray(resultByteShare, 0, resultByteShare.Length);

            #endregion


            #region Cleanup work
            CryptoStreamMining[idThread] = new CryptoStream(MemoryStreamMining[idThread],
                CryptoTransformMining[idThread], CryptoStreamMode.Write);
            MemoryStreamMining[idThread].SetLength(0);
            Array.Clear(resultByteShare, 0, resultByteShare.Length);
            Array.Clear(textBytes, 0, textBytes.Length);

            #endregion

            return result;
        }


        /// <summary>
        /// Convert a byte array to hex string like Bitconverter class.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetHexStringFromByteArray(byte[] value, int startIndex, int length)
        {
            int newSize = length * 3;
            char[] hexCharArray = new char[newSize];
            int currentIndex;
            for (currentIndex = 0; currentIndex < newSize; currentIndex += 3)
            {
                byte currentByte = value[startIndex++];
                hexCharArray[currentIndex] = GetHexValue(currentByte / 0x10);
                hexCharArray[currentIndex + 1] = GetHexValue(currentByte % 0x10);
                hexCharArray[currentIndex + 2] = '-';
            }
            return new string(hexCharArray, 0, hexCharArray.Length - 1);
        }

        /// <summary>
        /// Get Hex value from char index value.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private static char GetHexValue(int i)
        {
            if (i < 10)
            {
                return (char)(i + 0x30);
            }
            return (char)((i - 10) + 0x41);
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



        /// <summary>
        /// Generate a sha512 hash
        /// </summary>
        /// <param name="input"></param>
        /// <param name="idThread"></param>
        /// <returns></returns>
        public static string GenerateSha512FromString(string input, int idThread)
        {
            if (Sha512ManagedMining[idThread] == null)
            {
                Sha512ManagedMining[idThread] = new SHA512Managed();
            }

            var bytes = Encoding.UTF8.GetBytes(input);

           return ByteArrayToHexString(Sha512ManagedMining[idThread].ComputeHash(bytes));

        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            char[] hexChars = new char[(bytes.Length * 2)];
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 255;
                hexChars[j * 2] = HexArrayList[(int)((uint)v >> 4)];
                hexChars[(j * 2) + 1] = HexArrayList[v & 15];
            }
            return new string(hexChars);
        }
    }
}