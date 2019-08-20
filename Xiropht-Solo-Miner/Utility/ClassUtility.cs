using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Xiropht_Solo_Miner.ConsoleMiner;

namespace Xiropht_Solo_Miner.Utility
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
        /// Set automatic affinity, use native function depending of the Operating system.
        /// </summary>
        /// <param name="threadIdMining"></param>
        public static void SetAffinity(int threadIdMining)
        {
            try
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
                        ProcessThread thread = Process.GetCurrentProcess().Threads.Cast<ProcessThread>()
                            .Single(t => t.Id == threadId);

                        ulong cpuMask = 1UL << threadIdMining;

                        thread.ProcessorAffinity = (IntPtr) cpuMask;
                        SetThreadAffinityMask((IntPtr) threadIdMining, (IntPtr) cpuMask);

                    }
                }
            }
            catch (Exception error)
            {
                ClassConsole.WriteLine(
                    "Cannot apply Automatic Affinity with thread id: " + threadIdMining + " | Exception: " + error.Message, 3);
            }
        }

        /// <summary>
        /// Set manual affinity, use native function depending of the Operating system.
        /// </summary>
        /// <param name="threadAffinity"></param>
        public static void SetManualAffinity(string threadAffinity)
        {
            try
            {
                ulong handle = Convert.ToUInt64(threadAffinity, 16);

                if (Environment.OSVersion.Platform == PlatformID.Unix) // Linux/UNIX
                {
                    sched_setaffinity(0, new IntPtr(sizeof(ulong)), ref handle);

                }
                else
                {

                    Thread.BeginThreadAffinity();
                    int threadId = GetCurrentThreadId();
                    ProcessThread thread = Process.GetCurrentProcess().Threads.Cast<ProcessThread>()
                        .Single(t => t.Id == threadId);
                    thread.ProcessorAffinity = (IntPtr) handle;
                    SetThreadAffinityMask((IntPtr) threadId, (IntPtr) handle);
                }
            }
            catch (Exception error)
            {
                ClassConsole.WriteLine(
                    "Cannot apply Manual Affinity with: " + threadAffinity + " | Exception: " + error.Message, 3);
            }
        }
    }

    public class ClassUtility
    {

        #region Math functions
        public static string[] RandomOperatorCalculation = {"+", "*", "%", "-", "/"};

        private static char[] randomNumberCalculation = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};


        [ThreadStatic] private static RNGCryptoServiceProvider GeneratorRngNormal;
        [ThreadStatic] private static RNGCryptoServiceProvider GeneratorRngSize;
        [ThreadStatic] private static RNGCryptoServiceProvider GeneratorRngInteger;
        [ThreadStatic] private static RNGCryptoServiceProvider GeneratorRngJob;
        [ThreadStatic] private static StringBuilder numberBuilder;


        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <returns></returns>
        public static int GetRandom()
        {

            if (GeneratorRngNormal == null)
            {
                GeneratorRngNormal = new RNGCryptoServiceProvider();
            }

            var randomByte = new byte[sizeof(int)];

            GeneratorRngNormal.GetBytes(randomByte);

            return BitConverter.ToInt32(randomByte, 0);
        }

        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetweenSize(int minimumValue, int maximumValue)
        {

            if (GeneratorRngSize == null)
            {
                GeneratorRngSize = new RNGCryptoServiceProvider();
            }

            byte[] randomByteSize = new byte[1];

            GeneratorRngSize.GetBytes(randomByteSize);

            var asciiValueOfRandomCharacter = Convert.ToDouble(randomByteSize[0]);

            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = Math.Floor(multiplier * range);

            return (int) (minimumValue + randomValueInRange);

        }


        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {

            if (GeneratorRngInteger == null)
            {
                GeneratorRngInteger = new RNGCryptoServiceProvider();
            }

            byte[] randomByteSize = new byte[sizeof(int)];

            GeneratorRngInteger.GetBytes(randomByteSize);

            var asciiValueOfRandomCharacter =
                Convert.ToDouble(randomByteSize[GetRandomBetweenSize(0, randomByteSize.Length - 1)]);

            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = Math.Floor(multiplier * range);

            return (int) (minimumValue + randomValueInRange);

        }


        /// <summary>
        /// Get a random number in float size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static decimal GetRandomBetweenJob(decimal minimumValue, decimal maximumValue)
        {

            if (GeneratorRngJob == null)
            {
                GeneratorRngJob = new RNGCryptoServiceProvider();
            }

            byte[] randomByteSize = new byte[sizeof(decimal)];

            GeneratorRngJob.GetBytes(randomByteSize);

            var asciiValueOfRandomCharacter =
                Convert.ToDecimal(randomByteSize[GetRandomBetweenSize(0, randomByteSize.Length - 1)]);

            var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255m - 0.00000000001m);

            var range = maximumValue - minimumValue + 1;

            var randomValueInRange = Math.Floor(multiplier * range);

            return (minimumValue + randomValueInRange);

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
            decimal number1 = decimal.Parse(firstNumber);
            decimal number2 = decimal.Parse(secondNumber);
            try
            {
                switch (operatorCalculation)
                {
                    case "+":
                        return number1 + number2;
                    case "-":
                        if (number1 > number2)
                        {
                            return number1 - number2;
                        }

                        break;
                    case "*":
                        return number1 * number2;
                    case "%":
                        if (number2 > number1)
                        {
                            return number2;
                        }

                        return number1 % number2;
                    case "/":
                        if (number1 >= number2)
                        {
                            return number1 / number2;
                        }

                        break;
                }
            }
            catch
            {

            }

            return 0;
        }

        #endregion


        /// <summary>
        /// Return a number for complete a math calculation text.
        /// </summary>
        /// <returns></returns>
        public static string GenerateNumberMathCalculation(decimal minRange, decimal maxRange)
        {
            if (numberBuilder == null)
            {
                numberBuilder = new StringBuilder();
            }
            else
            {
                numberBuilder.Clear();
            }

            int randomSize = GetRandomBetween(minRange.ToString("F0").Length, maxRange.ToString("F0").Length);
            int counter = 0;

            bool cleanGenerator = false;
            while (Program.CanMining)
            {

                numberBuilder.Append(randomNumberCalculation[GetRandomBetween(0, randomNumberCalculation.Length - 1)]);
                counter++;
                if (numberBuilder[0] == randomNumberCalculation[0])
                {
                    cleanGenerator = true;
                }

                if (counter == randomSize)
                {
                    if (decimal.TryParse(numberBuilder.ToString(), out var number))
                    {
                        return numberBuilder.ToString();
                    }

                    cleanGenerator = true;
                }

                if (cleanGenerator)
                {
                    numberBuilder.Clear();
                    counter = 0;
                    cleanGenerator = false;
                }
            }

            return numberBuilder.ToString();
        }


        #region Other functions 

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

        #endregion
    }
}
