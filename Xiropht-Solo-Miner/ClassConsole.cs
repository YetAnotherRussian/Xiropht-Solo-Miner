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
                    float accuratePourcent = 0;
                    if (Program.TotalHashrate != 0 && Program.TotalCalculation != 0)
                    {
                        accuratePourcent = ((float)Program.TotalHashrate / (float)Program.TotalCalculation) * 100;
                        accuratePourcent = (float)Math.Round(accuratePourcent, 2);
                    }
                    if (!Program.UseProxy)
                    {
                        WriteLine("Mining Speed: " + Program.TotalCalculation + " C/s | " + Program.TotalHashrate + " H/s | Accurate Rate " + accuratePourcent + "% > UNLOCK[" + Program.TotalBlockAccepted + "] REFUSED[" + Program.TotalBlockRefused + "]", 4);
                    }
                    else
                    {
                        if (Program.ProxyWantShare)
                        {
                            WriteLine("Mining Speed: " + Program.TotalCalculation + " C/s | " + Program.TotalHashrate + " H/s | Accurate Rate " + accuratePourcent + "% > GOOD[" + Program.TotalShareAccepted + "] INVALID[" + Program.TotalShareInvalid + "]", 4);
                        }
                        else
                        {
                            WriteLine("Mining Speed: " + Program.TotalCalculation + " C/s | " + Program.TotalHashrate + " H/s | Accurate Rate " + accuratePourcent + "% > UNLOCK[" + Program.TotalBlockAccepted + "] REFUSED[" + Program.TotalBlockRefused + "]", 4);
                        }
                    }
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
