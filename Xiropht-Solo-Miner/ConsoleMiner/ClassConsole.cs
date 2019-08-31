using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Xiropht_Solo_Miner.Utility;

namespace Xiropht_Solo_Miner.ConsoleMiner
{
    public class ClassConsole
    {
        private static PerformanceCounter _ramCounter;

        /// <summary>
        ///     Replace WriteLine function with forecolor system.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="color"></param>
        public static void WriteLine(string log, int color = 0)
        {
            switch (color)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 4:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 5:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
            }

            Console.WriteLine(DateTime.Now + " - " + log);
        }

        /// <summary>
        ///     Handle command line.
        /// </summary>
        /// <param name="command"></param>
        public static void CommandLine(string command)
        {
            switch (command.ToLower())
            {
                case "h":
                    if (Program.ClassMinerConfigObject.mining_show_calculation_speed)
                    {
                        WriteLine(
                            Program.TotalHashrate + " H/s | " + Program.TotalCalculation + " C/s > ACCEPTED[" +
                            Program.TotalBlockAccepted + "] REFUSED[" +
                            Program.TotalBlockRefused + "]", 4);
                    }
                    else
                    {
                        WriteLine(
                            Program.TotalHashrate + " H/s | ACCEPTED[" +
                            Program.TotalBlockAccepted + "] REFUSED[" +
                            Program.TotalBlockRefused + "]", 4);
                    }

                    break;
                case "d":
                    WriteLine("Current Block: " + Program.CurrentBlockId + " Difficulty: " +
                              Program.CurrentBlockDifficulty);
                    break;
                case "c":
                    if (Program.ClassMinerConfigObject.mining_enable_cache)
                    {
                        var allocationInMb = Process.GetCurrentProcess().PrivateMemorySize64 / 1e+6;
                        float availbleRam = 0;

                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            availbleRam = long.Parse(ClassUtility.RunCommandLineMemoryAvailable());
                        }
                        else
                        {
                            if (_ramCounter == null)
                            {
                                _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
                            }

                            availbleRam = _ramCounter.NextValue();
                        }

                        WriteLine("Current math combinaisons cached: " +  Program.DictionaryCacheMining.Count.ToString("F0") + " | RAM Used: " + allocationInMb + " MB(s) | RAM Available: "+availbleRam+" MB(s).");
                    }

                    break;
                case "r":
                    WriteLine("Current Range: " + Program.CurrentBlockJob.Replace(";", "|"));
                    break;
            }
        }
    }
}