using System;
using System.Security.Cryptography;
using System.Text;

namespace Xiropht_Solo_Miner
{
    public class ClassUtils
    {

        public static string[] randomOperatorCalculation = new[] { "+", "*", "%", "-", "/" };

        private static string[] randomNumberCalculation = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {
            using (RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(int)];

                Generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (int)(minimumValue + randomValueInRange);
            }
        }

        /// <summary>
        /// Get a random number in float size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static float GetRandomBetweenJob(float minimumValue, float maximumValue)
        {
            using (RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(float)];

                Generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = (float)Convert.ToDouble(randomNumber[0]);

                var multiplier = (float)Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = (float)Math.Floor(multiplier * range);
                return (minimumValue + randomValueInRange);
            }
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

        /// <summary>
        /// Return result from a math calculation.
        /// </summary>
        /// <param name="firstNumber"></param>
        /// <param name="operatorCalculation"></param>
        /// <param name="secondNumber"></param>
        /// <returns></returns>
        public static float ComputeCalculation(string firstNumber, string operatorCalculation, string secondNumber)
        {
            float calculCompute = 0;
            if (operatorCalculation.Contains("+"))
            {
                calculCompute = float.Parse(firstNumber) + float.Parse(secondNumber);
                calculCompute = (float)Math.Round(calculCompute, 0);
            }
            else if (operatorCalculation.Contains("*"))
            {
                calculCompute = float.Parse(firstNumber) * float.Parse(secondNumber);
                calculCompute = (float)Math.Round(calculCompute, 0);
            }
            else if (operatorCalculation.Contains("%"))
            {
                calculCompute = float.Parse(firstNumber) % float.Parse(secondNumber);
                calculCompute = (float)Math.Round(calculCompute, 0);
            }
            else if (operatorCalculation.Contains("-"))
            {
                calculCompute = float.Parse(firstNumber) - float.Parse(secondNumber);
                calculCompute = (float)Math.Round(calculCompute, 0);
            }
            else if (operatorCalculation.Contains("/"))
            {
                calculCompute = float.Parse(firstNumber) / float.Parse(secondNumber);
                calculCompute = (float)Math.Round(calculCompute, 0);
            }
            return calculCompute;
        }

        /// <summary>
        /// Return a number for complete a math calculation text.
        /// </summary>
        /// <returns></returns>
        public static string GenerateNumberMathCalculation(float minRange, float maxRange, int currentBlockDifficultyLength)
        {
            string number = "0";
            StringBuilder numberBuilder = new StringBuilder();
            while (float.Parse(number) > maxRange || float.Parse(number) <= 1 || number.Length >= currentBlockDifficultyLength)
            {
                var randomJobSize = ("" + GetRandomBetweenJob(minRange, maxRange)).Length;

                int randomSize = GetRandomBetween(1, randomJobSize);
                int counter = 0;
                while (counter < randomSize)
                {
                    if (randomSize > 1)
                    {
                        var numberRandom = randomNumberCalculation[GetRandomBetween(0, randomNumberCalculation.Length - 1)];
                        if (counter == 0)
                        {
                            while (numberRandom == "0")
                            {
                                numberRandom = randomNumberCalculation[GetRandomBetween(0, randomNumberCalculation.Length - 1)];
                            }
                            numberBuilder.Append(numberRandom);
                        }
                        else
                        {
                            numberBuilder.Append(numberRandom);
                        }
                    }
                    else
                    {
                        numberBuilder.Append(
                                       randomNumberCalculation[
                                           GetRandomBetween(0, randomNumberCalculation.Length - 1)]);
                    }
                    counter++;
                }
                number = numberBuilder.ToString();
                numberBuilder.Clear();
                return number;
            }
            return number;
        }
    }
}
