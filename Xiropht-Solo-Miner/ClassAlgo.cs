using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xiropht_Solo_Miner
{

    public class ClassAlgo
    {
        public static string EncryptAesShare(string text, byte[] aesKeyBytes, byte[] aesIvBytes, int size)
        {
            if (Program.IsLinux)
            {
                using (var aes = new AesCryptoServiceProvider
                {
                    BlockSize = size,
                    KeySize = size,
                    Key = aesKeyBytes,
                    IV = aesIvBytes
                })
                {

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var textBytes = Encoding.UTF8.GetBytes(text);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                            {
                                cs.Write(textBytes, 0, textBytes.Length);
                            }
                            return BitConverter.ToString(ms.ToArray());
                        }
                    }
                }
            }
            else
            {
                using (var aes = new AesManaged
                {
                    BlockSize = size,
                    KeySize = size,
                    Key = aesKeyBytes,
                    IV = aesIvBytes
                })
                {

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var textBytes = Encoding.UTF8.GetBytes(text);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                            {
                                cs.Write(textBytes, 0, textBytes.Length);
                            }
                            return BitConverter.ToString(ms.ToArray());
                        }
                    }
                }
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