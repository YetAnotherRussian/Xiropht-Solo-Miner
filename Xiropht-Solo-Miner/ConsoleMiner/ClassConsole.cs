using System;

namespace Xiropht_Solo_Miner.ConsoleMiner
{
    public class ClassConsole
    {
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
                case "r":
                    WriteLine("Current Range: " + Program.CurrentBlockJob.Replace(";", "|"));
                    break;
            }
        }
    }
}