using System;

namespace Xiropht_Solo_Miner
{
    public class ClassConsole
    {
        /// <summary>
        /// Replace WriteLine function with forecolor system.
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
            }
            Console.WriteLine(DateTime.Now + " - " + log);
        }

        /// <summary>
        /// Handle command line.
        /// </summary>
        /// <param name="command"></param>
        public static void CommandLine(string command)
        {
            switch (command.ToLower())
            {
                case "h":
                    WriteLine(Program.TotalHashrate + " H/s > UNLOCKED[" + Program.TotalBlockAccepted + "] REFUSED[" + Program.TotalBlockRefused + "]", 4);
                    break;
                case "d":
                    WriteLine("Current Block: " + Program.CurrentBlockId + " Difficulty: " + Program.CurrentBlockDifficulty);
                    break;
                case "r":
                    WriteLine("Current Range: " + Program.CurrentBlockJob.Replace(";", "|"));
                    break;
            }
        }
    }
}
