using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Xiropht_Solo_Miner
{

    public class ClassUtilityAffinity
    {
        [DllImport("libc.so.6", SetLastError = true)]
        private static extern int sched_setaffinity(int pid, IntPtr cpusetsize, ref ulong cpuset);

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

        /// <summary>
        /// Set affinity, use native function depending of the Operating system.
        /// </summary>
        /// <param name="processorID"></param>
        public static void SetAffinity(int threadIdMining)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) // Linux/UNIX
            {
                if (Environment.ProcessorCount > threadIdMining && threadIdMining >= 0)
                {
                    ulong processorMask = 1UL << threadIdMining;
                    sched_setaffinity(0, new IntPtr(sizeof(ulong)), ref processorMask);
                }
            }
            else
            {
                if (Environment.ProcessorCount > threadIdMining && threadIdMining >= 0)
                {
                    Thread.BeginThreadAffinity();
                    int threadId = GetCurrentThreadId();
                    ProcessThread thread = Process.GetCurrentProcess().Threads.Cast<ProcessThread>().Where(t => t.Id == threadId).Single();

                    ulong cpuMask = 1UL << threadIdMining;

                    thread.ProcessorAffinity = (IntPtr)cpuMask;
                }
            }
        }
    }

    public class ClassUtility
    {

        public static string[] randomOperatorCalculation = new[] { "+", "*", "%", "-", "/" };

        private static string[] randomNumberCalculation = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private static readonly char[] HexArray = "0123456789ABCDEF".ToCharArray();

        private static RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider();

        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {

            var randomNumber = new byte[sizeof(int)];

            Generator.GetBytes(randomNumber);

            var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);

        }

        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetweenMulti(int minimumValue, int maximumValue, int totalRound)
        {
            int randomNumberGenerated = 0;

            for (int i = 0; i < totalRound; i++)
            {
                var randomNumber = new byte[sizeof(int)];

                Generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                randomNumberGenerated = (int)(minimumValue + randomValueInRange);
                if (randomNumberGenerated < minimumValue || randomNumberGenerated > maximumValue)
                {
                    i--;
                }
            }
            return randomNumberGenerated;

        }

        /// <summary>
        /// Remove special characters
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get a random number in float size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static decimal GetRandomBetweenJob(decimal minimumValue, decimal maximumValue)
        {

            var randomNumber = new byte[sizeof(decimal)];

            Generator.GetBytes(randomNumber);

            var asciiValueOfRandomCharacter = (decimal)Convert.ToDouble(randomNumber[0]);

            var multiplier = (decimal)Math.Max(0, asciiValueOfRandomCharacter / 255m - 0.00000000001m);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = (decimal)Math.Floor(multiplier * range);
            return (minimumValue + randomValueInRange);

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
        public static decimal ComputeCalculation(string firstNumber, string operatorCalculation, string secondNumber)
        {
            decimal calculCompute = 0;
            try
            {
                switch (operatorCalculation)
                {
                    case "+":
                        calculCompute = decimal.Parse(firstNumber) + decimal.Parse(secondNumber);
                        break;
                    case "-":
                        calculCompute = decimal.Parse(firstNumber) - decimal.Parse(secondNumber);
                        break;
                    case "*":
                        calculCompute = decimal.Parse(firstNumber) * decimal.Parse(secondNumber);
                        break;
                    case "%":
                        calculCompute = decimal.Parse(firstNumber) % decimal.Parse(secondNumber);
                        break;
                    case "/":
                        calculCompute = decimal.Parse(firstNumber) / decimal.Parse(secondNumber);
                        break;
                }
            }
            catch
            {

            }

            return calculCompute;
        }

        /// <summary>
        /// Return a number for complete a math calculation text.
        /// </summary>
        /// <returns></returns>
        public static string GenerateNumberMathCalculation(decimal minRange, decimal maxRange, int currentBlockDifficultyLength)
        {
            string number = "0";
            StringBuilder numberBuilder = new StringBuilder();


            while (decimal.Parse(number) > maxRange || decimal.Parse(number) < minRange || number.Length > currentBlockDifficultyLength)
            {
                number = "0";
                var randomJobSize = GetRandomBetweenJob(minRange, maxRange).ToString("F0").Length;

                int randomSize = GetRandomBetween(1, randomJobSize);
                int counter = 0;
                while (counter < randomSize)
                {

                    numberBuilder.Append(randomNumberCalculation[GetRandomBetweenMulti(0, randomNumberCalculation.Length - 1, GetRandomBetween(1, 10))]);

                    counter++;
                }
                number = numberBuilder.ToString();
                numberBuilder.Clear();
            }
            return number;
        }

    }
}
