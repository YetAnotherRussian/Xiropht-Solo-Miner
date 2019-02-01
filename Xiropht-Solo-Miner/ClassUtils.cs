using System;
using System.Security.Cryptography;
using System.Text;
using NCalc;

namespace Xiropht_Solo_Miner
{
    public class ClassUtils
    {

        private static readonly RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider();

        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {
            var randomNumber = new byte[1];

            Generator.GetBytes(randomNumber);

            var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);
        }

        /// <summary>
        /// Get a random number in float size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static float GetRandomBetweenJob(float minimumValue, float maximumValue)
        {
            Random rand = new Random();
            string value = "" + Math.Round((minimumValue + rand.NextDouble() * (maximumValue - minimumValue)),0);
            return float.Parse(value);
        }

        /// <summary>
        /// Convert a string into hex string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static string StringToHex(string hex)
        {
            byte[] ba = Encoding.UTF8.GetBytes(hex);

            return BitConverter.ToString(ba).Replace("-", "");
        }

        /// <summary>
        /// Calculate a math calculation and return a result.
        /// </summary>
        /// <param name="number1"></param>
        /// <param name="number2"></param>
        /// <param name="operatorMath"></param>
        /// <returns></returns>
        public static float GetResultFromMathCalculation(string number1, string number2, string operatorMath)
        {
            Expression ex = new Expression(number1 + " " + operatorMath + " " + number2);

            var result = ex.Evaluate().ToString();
            var resultDouble = double.Parse(result);
            return (float)resultDouble;
        }

        /// <summary>
        /// Get a string from byte hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }


        /// <summary>
        /// Generate a hash SHA256
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GenerateSHA256(string data)
        {
            string hashResult = string.Empty;

            if (data != null)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] dataBuffer = Encoding.UTF8.GetBytes(data);
                    byte[] dataBufferHashed = sha256.ComputeHash(dataBuffer);
                    hashResult = GetStringFromHash(dataBufferHashed);
                }
            }
            return hashResult;
        }

        /// <summary>
        /// Generate a sha512 hash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GenerateSHA512(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }
        }


    }
}
