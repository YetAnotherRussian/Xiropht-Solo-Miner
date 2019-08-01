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
        /// Set automatic affinity, use native function depending of the Operating system.
        /// </summary>
        /// <param name="threadIdMining"></param>
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
                    SetThreadAffinityMask((IntPtr)threadIdMining, (IntPtr)cpuMask);

                }
            }
        }

        /// <summary>
        /// Set manual affinity, use native function depending of the Operating system.
        /// </summary>
        /// <param name="processorID"></param>
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
                    ProcessThread thread = Process.GetCurrentProcess().Threads.Cast<ProcessThread>().Where(t => t.Id == threadId).Single();
                    thread.ProcessorAffinity = (IntPtr)handle;
                    SetThreadAffinityMask((IntPtr)threadId, (IntPtr)handle);
                }
            }
            catch (Exception error)
            {
                ClassConsole.WriteLine("Cannot enable Manual Affinity with: " + threadAffinity + " | Exception: " + error.Message, 3);
            }
        }
    }

    public class ClassUtility
    {

        public static string[] randomOperatorCalculation = new[] { "+", "*", "%", "-", "/" };

        private static char[] randomNumberCalculation = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static readonly char[] HexArray = "0123456789ABCDEF".ToCharArray();

        [ThreadStatic]
        private static XorShiftRandom Generator;

        [ThreadStatic]
        private static RNGCryptoServiceProvider GeneratorRng;



        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandom()
        {

            if (GeneratorRng == null)
            {
                GeneratorRng = new RNGCryptoServiceProvider();
            }
            var randomByte = new byte[sizeof(int)];

            GeneratorRng.GetBytes(randomByte);

            return BitConverter.ToInt32(randomByte, 0);
        }


        /// <summary>
        ///     Get a random number in integer size.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {

            bool useXorRandom = GetRandom() >= GetRandom();

            if (useXorRandom)
            {
                if (Generator == null)
                {
                    Generator = new XorShiftRandom();
                }
                byte[] randomByteSize = new byte[sizeof(int)];

                Generator.NextBytes(randomByteSize);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomByteSize[new Random().Next(0, randomByteSize.Length - 1)]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (int)(minimumValue + randomValueInRange);
            }
            else
            {
                if (GeneratorRng == null)
                {
                    GeneratorRng = new RNGCryptoServiceProvider();
                }
                byte[] randomByteSize = new byte[sizeof(int)];

                GeneratorRng.GetBytes(randomByteSize);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomByteSize[new Random().Next(0, randomByteSize.Length - 1)]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (int)(minimumValue + randomValueInRange);
            }

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
            bool useXorRandom = GetRandom() >= GetRandom();

            if (useXorRandom)
            {
                if (Generator == null)
                {
                    Generator = new XorShiftRandom();
                }
                byte[] randomByteSize = new byte[sizeof(decimal)];

                Generator.NextBytes(randomByteSize);

                var asciiValueOfRandomCharacter = Convert.ToDecimal(randomByteSize[new Random().Next(0, randomByteSize.Length - 1)]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255m - 0.00000000001m);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (minimumValue + randomValueInRange);
            }
            else
            {
                if (GeneratorRng == null)
                {
                    GeneratorRng = new RNGCryptoServiceProvider();
                }
                byte[] randomByteSize = new byte[sizeof(decimal)];

                GeneratorRng.GetBytes(randomByteSize);

                var asciiValueOfRandomCharacter = Convert.ToDecimal(randomByteSize[new Random().Next(0, randomByteSize.Length - 1)]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255m - 0.00000000001m);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

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
        public static string GenerateNumberMathCalculation(decimal minRange, decimal maxRange)
        {
            string number = "0";
            StringBuilder numberBuilder = new StringBuilder();


            int randomSize = GetRandomBetween(minRange.ToString("F0").Length, maxRange.ToString("F0").Length);
            int counter = 0;
            while (Program.CanMining)
            {

                numberBuilder.Append(randomNumberCalculation[GetRandomBetween(0, randomNumberCalculation.Length - 1)]);
                if (numberBuilder.ToString() == "0" && counter == 0)
                {
                    numberBuilder.Clear();
                    counter = 0;
                }
                else
                {
                    counter++;
                    if (counter == randomSize)
                    {
                        break;
                    }
                }
            }
            number = numberBuilder.ToString();
            numberBuilder.Clear();

            return number;
        }

    }

    public class XorShiftRandom
    {

        #region Data Members

        // Constants
        private const double DOUBLE_UNIT = 1.0 / (int.MaxValue + 1.0);

        // State Fields
        private ulong x_;
        private ulong y_;

        // Buffer for optimized bit generation.
        private ulong buffer_;
        private ulong bufferMask_;

        #endregion

        #region Constructor

        /// <summary>
        ///   Constructs a new  generator using two
        ///   random Guid hash codes as a seed.
        /// </summary>
        public XorShiftRandom()
        {
            x_ = (ulong)Guid.NewGuid().GetHashCode();
            y_ = (ulong)Guid.NewGuid().GetHashCode();
        }

        /// <summary>
        ///   Constructs a new  generator
        ///   with the supplied seed.
        /// </summary>
        /// <param name="seed">
        ///   The seed value.
        /// </param>
        public XorShiftRandom(ulong seed)
        {
            x_ = seed << 3; x_ = seed >> 3;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Generates a pseudorandom boolean.
        /// </summary>
        /// <returns>
        ///   A pseudorandom boolean.
        /// </returns>
        public bool NextBoolean()
        {
            bool _;
            if (bufferMask_ > 0)
            {
                _ = (buffer_ & bufferMask_) == 0;
                bufferMask_ >>= 1;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            buffer_ = temp_y + y_;
            x_ = temp_x;
            y_ = temp_y;

            bufferMask_ = 0x8000000000000000;
            return (buffer_ & 0xF000000000000000) == 0;
        }

        /// <summary>
        ///   Generates a pseudorandom byte.
        /// </summary>
        /// <returns>
        ///   A pseudorandom byte.
        /// </returns>
        public byte NextByte()
        {
            if (bufferMask_ >= 8)
            {
                byte _ = (byte)buffer_;
                buffer_ >>= 8;
                bufferMask_ >>= 8;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            buffer_ = temp_y + y_;
            x_ = temp_x;
            y_ = temp_y;

            bufferMask_ = 0x8000000000000;
            return (byte)(buffer_ >>= 8);
        }

        /// <summary>
        ///   Generates a pseudorandom 16-bit signed integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 16-bit signed integer.
        /// </returns>
        public short NextInt16()
        {
            short _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (short)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 16-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 16-bit unsigned integer.
        /// </returns>
        public ushort NextUInt16()
        {
            ushort _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (ushort)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 32-bit signed integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 32-bit signed integer.
        /// </returns>
        public int NextInt32()
        {
            int _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (int)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 32-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 32-bit unsigned integer.
        /// </returns>
        public uint NextUInt32()
        {
            uint _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (uint)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 64-bit signed integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 64-bit signed integer.
        /// </returns>
        public long NextInt64()
        {
            long _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (long)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 64-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 64-bit unsigned integer.
        /// </returns>
        public ulong NextUInt64()
        {
            ulong _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (ulong)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom double between
        ///   0 and 1 non-inclusive.
        /// </summary>
        /// <returns>
        ///   A pseudorandom double.
        /// </returns>
        public double NextDouble()
        {
            double _;
            ulong temp_x, temp_y, temp_z;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            temp_z = temp_y + y_;
            _ = DOUBLE_UNIT * (0x7FFFFFFF & temp_z);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom decimal between
        ///   0 and 1 non-inclusive.
        /// </summary>
        /// <returns>
        ///   A pseudorandom decimal.
        /// </returns>
        public decimal NextDecimal()
        {
            decimal _;
            int l, m, h;
            ulong temp_x, temp_y, temp_z;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            temp_z = temp_y + y_;

            h = (int)(temp_z & 0x1FFFFFFF);
            m = (int)(temp_z >> 16);
            l = (int)(temp_z >> 32);

            _ = new decimal(l, m, h, false, 28);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        /// <summary>
        ///   Fills the supplied buffer with pseudorandom bytes.
        /// </summary>
        /// <param name="buffer">
        ///   The buffer to fill.
        /// </param>
        public unsafe void NextBytes(byte[] buffer)
        {
            // Localize state for stack execution
            ulong x = x_, y = y_, temp_x, temp_y, z;

            fixed (byte* pBuffer = buffer)
            {
                ulong* pIndex = (ulong*)pBuffer;
                ulong* pEnd = (ulong*)(pBuffer + buffer.Length);

                // Fill array in 8-byte chunks
                while (pIndex <= pEnd - 1)
                {
                    temp_x = y;
                    x ^= x << 23; temp_y = x ^ y ^ (x >> 17) ^ (y >> 26);

                    *(pIndex++) = temp_y + y;

                    x = temp_x;
                    y = temp_y;
                }

                // Fill remaining bytes individually to prevent overflow
                if (pIndex < pEnd)
                {
                    temp_x = y;
                    x ^= x << 23; temp_y = x ^ y ^ (x >> 17) ^ (y >> 26);
                    z = temp_y + y;

                    byte* pByte = (byte*)pIndex;
                    while (pByte < pEnd) *(pByte++) = (byte)(z >>= 8);
                }
            }

            // Store modified state in fields.
            x_ = x;
            y_ = y;
        }

        #endregion

    }
}
