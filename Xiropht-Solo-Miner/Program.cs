using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Seed;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.SoloMining;
using Xiropht_Connector_All.Utils;

namespace Xiropht_Solo_Miner
{
    class Program
    {
        /// <summary>
        /// For mining method.
        /// </summary>
        public static List<string> ListeMiningMethodName = new List<string>();
        public static List<string> ListeMiningMethodContent = new List<string>();

        /// <summary>
        /// For mining stats.
        /// </summary>
        public static int TotalBlockAccepted;
        public static int TotalBlockRefused;
        private const int HashrateIntervalCalculation = 10;

        /// <summary>
        /// Current block information for mining it.
        /// </summary>
        public static string CurrentBlockId;
        public static string CurrentBlockHash;
        public static string CurrentBlockAlgorithm;
        public static string CurrentBlockSize;
        public static string CurrentBlockMethod;
        public static string CurrentBlockKey;
        public static string CurrentBlockJob;
        public static string CurrentBlockReward;
        public static string CurrentBlockDifficulty;
        public static string CurrentBlockTimestampCreate;
        public static string CurrentBlockIndication;


        /// <summary>
        /// For network.
        /// </summary>
        private static ClassSeedNodeConnector ObjectSeedNodeNetwork;
        private static string CertificateConnection;
        private static bool IsConnected;
        public static bool CanMining;
        private static bool LoginAccepted;
        public static int TotalShareAccepted;
        public static int TotalShareInvalid;
        public static long LastPacketReceived;
        private const int TimeoutPacketReceived = 60; // Max 60 seconds.
        private static int PacketSpeedSend;


        /// <summary>
        /// Threading.
        /// </summary>
        private const int ThreadCheckNetworkInterval = 1 * 1000; // Check each 5 seconds.
        private static Thread ThreadListenNetwork;
        private static Thread ThreadCheckNetwork;
        private static Thread ThreadMiningMethod;
        private static Thread[] ThreadMining;
        private static CancellationTokenSource cts;
        public static List<int> TotalMiningHashrateRound = new List<int>();
        public static float TotalHashrate;
        private static bool CheckConnectionStarted;

        /// <summary>
        /// Encryption informations and objects.
        /// </summary>
        private static byte[] CurrentAesKeyBytes;
        private static byte[] CurrentAesIvBytes;
        public static int CurrentRoundAesRound;
        public static int CurrentRoundAesSize;
        public static string CurrentRoundAesKey;
        public static int CurrentRoundXorKey;

        /// <summary>
        /// Wallet mining.
        /// </summary>
        private static bool CalculateHashrateEnabled;
        private const int TotalConfigLine = 9;
        private static string MalformedPacket;
        private const string OldConfigFile = "\\config.ini";
        private const string ConfigFile = "\\config.json";
        private static ClassMinerConfig ClassMinerConfigObject;
        private static Dictionary<int, Dictionary<string, string>> DictionaryCacheMining = new Dictionary<int, Dictionary<string, string>>();
        public static bool IsLinux;

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        [MTAThread]
        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
            ClassConsole.WriteLine("Xiropht Solo Miner - " + Assembly.GetExecutingAssembly().GetName().Version + "R", 4);

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args2)
            {
                var filePath = ConvertPath(AppDomain.CurrentDomain.BaseDirectory + "\\error_miner.txt");
                var exception = (Exception)args2.ExceptionObject;
                using (var writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine("Message :" + exception.Message + "<br/>" + Environment.NewLine +
                                     "StackTrace :" +
                                     exception.StackTrace +
                                     "" + Environment.NewLine + "Date :" + DateTime.Now);
                    writer.WriteLine(Environment.NewLine +
                                     "-----------------------------------------------------------------------------" +
                                     Environment.NewLine);
                }

                Trace.TraceError(exception.StackTrace);

                Environment.Exit(1);

            };

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                IsLinux = true;
            }

            if (File.Exists(GetCurrentPathConfig(OldConfigFile)))
            {
                if (LoadConfig(true))
                {
                    ThreadMining = new Thread[ClassMinerConfigObject.mining_thread];
                    TotalMiningHashrateRound = new List<int>();
                    for (int i = 0; i < ClassMinerConfigObject.mining_thread; i++)
                    {
                        if (i < ClassMinerConfigObject.mining_thread)
                        {
                            TotalMiningHashrateRound.Add(0);
                        }
                    }

                    Console.WriteLine("Connecting to the network..");
                    new Thread(async () => await StartConnectMinerAsync()).Start();
                }
                else
                {
                    ClassConsole.WriteLine("config file invalid, do you want to follow instructions to setting again your config file ? [Y/N]", 2);
                    string choose = Console.ReadLine();
                    if (choose.ToLower() == "y")
                    {
                        FirstSettingConfig();
                    }
                    else
                    {
                        ClassConsole.WriteLine("Close solo miner program.", 0);
                        Process.GetCurrentProcess().Kill();
                    }
                }
            }
            else
            {
                if (File.Exists(GetCurrentPathConfig(ConfigFile)))
                {
                    if (LoadConfig(false))
                    {
                        ThreadMining = new Thread[ClassMinerConfigObject.mining_thread];
                        TotalMiningHashrateRound = new List<int>();
                        for (int i = 0; i < ClassMinerConfigObject.mining_thread; i++)
                        {
                            if (i < ClassMinerConfigObject.mining_thread)
                            {
                                TotalMiningHashrateRound.Add(0);
                            }
                        }

                        Console.WriteLine("Connecting to the network..");
                        new Thread(async () => await StartConnectMinerAsync()).Start();
                    }
                    else
                    {
                        ClassConsole.WriteLine("config file invalid, do you want to follow instructions to setting again your config file ? [Y/N]", 2);
                        string choose = Console.ReadLine();
                        if (choose.ToLower() == "y")
                        {
                            FirstSettingConfig();
                        }
                        else
                        {
                            ClassConsole.WriteLine("Close solo miner program.", 0);
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                }
                else
                {
                    FirstSettingConfig();
                }
            }

            ClassConsole.WriteLine("Command Line: h -> show hashrate.", 4);
            ClassConsole.WriteLine("Command Line: d -> show current difficulty.", 4);
            ClassConsole.WriteLine("Command Line: r -> show current range", 4);


            Console.CancelKeyPress += Console_CancelKeyPress;

            var threadCommand = new Thread(delegate ()
            {
                string command = string.Empty;
                while (true)
                {
                    StringBuilder input = new StringBuilder();
                    var key = Console.ReadKey(true);
                    input.Append(key.KeyChar);
                    ClassConsole.CommandLine(input.ToString());
                    input.Clear();
                }
            });
            threadCommand.Start();
        }

        /// <summary>
        /// First time to setting config file.
        /// </summary>
        private static void FirstSettingConfig()
        {
            ClassMinerConfigObject = new ClassMinerConfig();
            ClassConsole.WriteLine("Do you want to use a proxy instead seed node? [Y/N]", 2);
            var choose = Console.ReadLine();

            if (choose.ToLower() == "y")
            {
                Console.WriteLine("Please, write your wallet address or a worker name to start your solo mining: ");
                ClassMinerConfigObject.mining_wallet_address = Console.ReadLine();

                Console.WriteLine("Write the IP/HOST of your mining proxy: ");
                ClassMinerConfigObject.mining_proxy_host = Console.ReadLine();
                Console.WriteLine("Write the port of your mining proxy: ");

                while (!int.TryParse(Console.ReadLine(), out ClassMinerConfigObject.mining_proxy_port))
                {
                    Console.WriteLine("This is not a port number, please try again: ");
                }
                Console.WriteLine("Do you want select a mining range percentage of difficulty? [Y/N]");
                choose = Console.ReadLine();
                if (choose.ToLower() == "y")
                {
                    Console.WriteLine("Select the start percentage range of difficulty [0 to 100]:");
                    while (!int.TryParse(Console.ReadLine(), out ClassMinerConfigObject.mining_percent_difficulty_start))
                    {
                        Console.WriteLine("This is not a number, please try again: ");
                    }
                    if (ClassMinerConfigObject.mining_percent_difficulty_start > 100)
                    {
                        ClassMinerConfigObject.mining_percent_difficulty_start = 100;
                    }
                    if (ClassMinerConfigObject.mining_percent_difficulty_start < 0)
                    {
                        ClassMinerConfigObject.mining_percent_difficulty_start = 0;
                    }

                    Console.WriteLine("Select the end percentage range of difficulty [" + ClassMinerConfigObject.mining_percent_difficulty_start + " to 100]: ");
                    while (!int.TryParse(Console.ReadLine(), out ClassMinerConfigObject.mining_percent_difficulty_end))
                    {
                        Console.WriteLine("This is not a number, please try again: ");
                    }
                    if (ClassMinerConfigObject.mining_percent_difficulty_end < 1)
                    {
                        ClassMinerConfigObject.mining_percent_difficulty_end = 1;
                    }
                    else if (ClassMinerConfigObject.mining_percent_difficulty_end > 100)
                    {
                        ClassMinerConfigObject.mining_percent_difficulty_end = 100;
                    }

                    if (ClassMinerConfigObject.mining_percent_difficulty_start > ClassMinerConfigObject.mining_percent_difficulty_end)
                    {
                        ClassMinerConfigObject.mining_percent_difficulty_start -= (ClassMinerConfigObject.mining_percent_difficulty_start - ClassMinerConfigObject.mining_percent_difficulty_end);
                    }
                    else
                    {
                        if (ClassMinerConfigObject.mining_percent_difficulty_start == ClassMinerConfigObject.mining_percent_difficulty_end)
                        {
                            ClassMinerConfigObject.mining_percent_difficulty_start--;
                        }
                    }
                    if (ClassMinerConfigObject.mining_percent_difficulty_end < ClassMinerConfigObject.mining_percent_difficulty_start)
                    {
                        var tmpPercentStart = ClassMinerConfigObject.mining_percent_difficulty_start;
                        ClassMinerConfigObject.mining_percent_difficulty_start = ClassMinerConfigObject.mining_percent_difficulty_end;
                        ClassMinerConfigObject.mining_percent_difficulty_end = tmpPercentStart;
                    }
                }

                ClassMinerConfigObject.mining_enable_proxy = true;
            }
            else
            {

                Console.WriteLine("Please, write your wallet address to start your solo mining: ");
                ClassMinerConfigObject.mining_wallet_address = Console.ReadLine();
                ClassMinerConfigObject.mining_wallet_address = ClassUtility.RemoveSpecialCharacters(ClassMinerConfigObject.mining_wallet_address);

                ClassConsole.WriteLine("Checking wallet address..", 4);
                bool checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(ClassMinerConfigObject.mining_wallet_address).Result;

                while (ClassMinerConfigObject.mining_wallet_address.Length < ClassConnectorSetting.MinWalletAddressSize || ClassMinerConfigObject.mining_wallet_address.Length > ClassConnectorSetting.MaxWalletAddressSize || !checkWalletAddress)
                {
                    Console.WriteLine("Invalid wallet address - Please, write your wallet address to start your solo mining: ");
                    ClassMinerConfigObject.mining_wallet_address = Console.ReadLine();
                    ClassMinerConfigObject.mining_wallet_address = ClassUtility.RemoveSpecialCharacters(ClassMinerConfigObject.mining_wallet_address);
                    ClassConsole.WriteLine("Checking wallet address..", 4);
                    checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(ClassMinerConfigObject.mining_wallet_address).Result;
                }
                if (checkWalletAddress)
                {
                    ClassConsole.WriteLine("Wallet address: " + ClassMinerConfigObject.mining_wallet_address + " is valid.", 1);
                }
            }
            Console.WriteLine("How many threads do you want to run? Number of cores detected: " + Environment.ProcessorCount);

            var tmp = Console.ReadLine();
            if (!int.TryParse(tmp, out ClassMinerConfigObject.mining_thread))
            {
                ClassMinerConfigObject.mining_thread = Environment.ProcessorCount;
            }
            ThreadMining = new Thread[ClassMinerConfigObject.mining_thread];


            Console.WriteLine("Do you want share job range per thread ? [Y/N]");
            choose = Console.ReadLine();
            if (choose.ToLower() == "y")
            {
                ClassMinerConfigObject.mining_thread_spread_job = true;
            }
            for (int i = 0; i < ClassMinerConfigObject.mining_thread; i++)
            {
                if (i < ClassMinerConfigObject.mining_thread)
                {
                    TotalMiningHashrateRound.Add(0);
                }
            }

            Console.WriteLine("Select thread priority: 0 = Lowest, 1 = BelowNormal, 2 = Normal, 3 = AboveNormal, 4 = Highest [Default: 2]:");

            if (!int.TryParse(Console.ReadLine(), out ClassMinerConfigObject.mining_thread_priority))
            {
                ClassMinerConfigObject.mining_thread_priority = 2;
            }


            WriteMinerConfig();
            if (ClassMinerConfigObject.mining_enable_cache)
            {
                InitializeMiningCache();
            }
            Console.WriteLine("Start to connect to the network..");
            new Thread(async () => await StartConnectMinerAsync()).Start();
        }

        /// <summary>
        /// Load config file.
        /// </summary>
        /// <returns></returns>
        private static bool LoadConfig(bool oldConfigType)
        {
            string configContent = string.Empty;

            if (oldConfigType)
            {
                ClassMinerConfigObject = new ClassMinerConfig();

                using (StreamReader reader = new StreamReader(GetCurrentPathConfig(OldConfigFile)))
                {
                    string line = string.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        configContent += line + "\n";
                    }
                }
                if (!string.IsNullOrEmpty(configContent))
                {
                    var splitConfigContent = configContent.Split(new[] { "\n" }, StringSplitOptions.None);

                    bool proxyLineFound = false;
                    int totalConfigLine = 0;
                    // Check at first if the miner use a proxy.
                    foreach (var configLine in splitConfigContent)
                    {
                        if (configLine.Contains("PROXY_ENABLE=") && !proxyLineFound)
                        {
                            proxyLineFound = true;
                            if (configLine.Replace("PROXY_ENABLE=", "").ToLower() == "y")
                            {
                                ClassMinerConfigObject.mining_enable_proxy = true;
                            }
                            totalConfigLine++;
                        }
                    }

                    if (proxyLineFound == false)
                    {
                        return false;
                    }

                    bool walletAddressCorrected = false;
                    bool walletAddressLineFound = false;
                    bool miningThreadLineFound = false;
                    foreach (var configLine in splitConfigContent)
                    {
                        if (configLine.Contains("WALLET_ADDRESS=") && !walletAddressLineFound)
                        {
                            walletAddressLineFound = true;
                            ClassMinerConfigObject.mining_wallet_address = configLine.Replace("WALLET_ADDRESS=", "");
                            if (!ClassMinerConfigObject.mining_enable_proxy)
                            {
                                ClassMinerConfigObject.mining_wallet_address = ClassUtility.RemoveSpecialCharacters(ClassMinerConfigObject.mining_wallet_address);
                                ClassConsole.WriteLine("Checking wallet address before to connect..", 4);

                                bool checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(ClassMinerConfigObject.mining_wallet_address).Result;
                                while (ClassMinerConfigObject.mining_wallet_address.Length < ClassConnectorSetting.MinWalletAddressSize || ClassMinerConfigObject.mining_wallet_address.Length > ClassConnectorSetting.MaxWalletAddressSize || !checkWalletAddress)
                                {
                                    Console.WriteLine("Invalid wallet address inside your config.ini file - Please, write your wallet address to start your solo mining: ");
                                    ClassMinerConfigObject.mining_wallet_address = Console.ReadLine();
                                    ClassMinerConfigObject.mining_wallet_address = ClassUtility.RemoveSpecialCharacters(ClassMinerConfigObject.mining_wallet_address);
                                    ClassConsole.WriteLine("Checking wallet address before to connect..", 4);
                                    walletAddressCorrected = true;
                                    checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(ClassMinerConfigObject.mining_wallet_address).Result;
                                }
                                if (checkWalletAddress)
                                {
                                    ClassConsole.WriteLine("Wallet address: " + ClassMinerConfigObject.mining_wallet_address + " is valid.", 1);
                                }
                            }
                            totalConfigLine++;
                        }
                        if (configLine.Contains("MINING_THREAD=") && !miningThreadLineFound)
                        {
                            miningThreadLineFound = true;
                            if (!int.TryParse(configLine.Replace("MINING_THREAD=", ""), out ClassMinerConfigObject.mining_thread))
                            {
                                ClassConsole.WriteLine("MINING_THREAD line contain an invalid integer value.", 3);
                                return false;
                            }
                            totalConfigLine++;
                        }
                        if (configLine.Contains("MINING_THREAD_PRIORITY="))
                        {
                            if (!int.TryParse(configLine.Replace("MINING_THREAD_PRIORITY=", ""), out ClassMinerConfigObject.mining_thread_priority))
                            {
                                ClassConsole.WriteLine("MINING_THREAD_PRIORITY= line contain an invalid integer value.", 3);
                                return false;
                            }
                            totalConfigLine++;
                        }
                        if (configLine.Contains("MINING_THREAD_SPREAD_JOB="))
                        {
                            if (configLine.Replace("MINING_THREAD_SPREAD_JOB=", "").ToLower() == "y")
                            {
                                ClassMinerConfigObject.mining_thread_spread_job = true;
                            }
                            totalConfigLine++;
                        }
                        if (configLine.Contains("PROXY_PORT="))
                        {
                            if (!int.TryParse(configLine.Replace("PROXY_PORT=", ""), out ClassMinerConfigObject.mining_proxy_port))
                            {
                                ClassConsole.WriteLine("PROXY_PORT= line contain an invalid integer value.", 3);
                                return false;
                            }
                            totalConfigLine++;
                        }
                        if (configLine.Contains("PROXY_HOST="))
                        {
                            ClassMinerConfigObject.mining_proxy_host = configLine.Replace("PROXY_HOST=", "");
                            totalConfigLine++;
                        }
                        if (configLine.Contains("MINING_PERCENT_DIFFICULTY_START="))
                        {
                            if (!int.TryParse(configLine.Replace("MINING_PERCENT_DIFFICULTY_START=", ""), out ClassMinerConfigObject.mining_percent_difficulty_start))
                            {
                                ClassConsole.WriteLine("MINING_PERCENT_DIFFICULTY_START= line contain an invalid integer value.", 3);
                                return false;
                            }
                            totalConfigLine++;
                        }
                        if (configLine.Contains("MINING_PERCENT_DIFFICULTY_END="))
                        {
                            if (!int.TryParse(configLine.Replace("MINING_PERCENT_DIFFICULTY_END=", ""), out ClassMinerConfigObject.mining_percent_difficulty_end))
                            {
                                ClassConsole.WriteLine("MINING_PERCENT_DIFFICULTY_END= line contain an invalid integer value.", 3);
                                return false;
                            }
                            totalConfigLine++;
                        }
                    }
                    if (totalConfigLine == TotalConfigLine)
                    {
                        if (walletAddressCorrected) // Resave config file.
                        {
                            WriteMinerConfig();
                        }
                        File.Delete(GetCurrentPathConfig(OldConfigFile));
                        WriteMinerConfig();
                        if (ClassMinerConfigObject.mining_enable_cache)
                        {
                            InitializeMiningCache();
                        }
                        return true;
                    }
                }
            }
            else
            {
                using (StreamReader reader = new StreamReader(GetCurrentPathConfig(ConfigFile)))
                {
                    string line = string.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        configContent += line;
                    }
                }
              
                    ClassMinerConfigObject = JsonConvert.DeserializeObject<ClassMinerConfig>(configContent);
               
                if (!ClassMinerConfigObject.mining_enable_proxy)
                {

                    ClassMinerConfigObject.mining_wallet_address = ClassUtility.RemoveSpecialCharacters(ClassMinerConfigObject.mining_wallet_address);
                    ClassConsole.WriteLine("Checking wallet address before to connect..", 4);
                    bool walletAddressCorrected = false;
                    bool checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(ClassMinerConfigObject.mining_wallet_address).Result;
                    while (ClassMinerConfigObject.mining_wallet_address.Length < ClassConnectorSetting.MinWalletAddressSize || ClassMinerConfigObject.mining_wallet_address.Length > ClassConnectorSetting.MaxWalletAddressSize || !checkWalletAddress)
                    {
                        Console.WriteLine("Invalid wallet address inside your config.ini file - Please, write your wallet address to start your solo mining: ");
                        ClassMinerConfigObject.mining_wallet_address = Console.ReadLine();
                        ClassMinerConfigObject.mining_wallet_address = ClassUtility.RemoveSpecialCharacters(ClassMinerConfigObject.mining_wallet_address);
                        ClassConsole.WriteLine("Checking wallet address before to connect..", 4);
                        walletAddressCorrected = true;
                        checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(ClassMinerConfigObject.mining_wallet_address).Result;
                    }
                    if (checkWalletAddress)
                    {
                        ClassConsole.WriteLine("Wallet address: " + ClassMinerConfigObject.mining_wallet_address + " is valid.", 1);
                    }
                    if (walletAddressCorrected)
                    {
                        WriteMinerConfig();
                    }

                    if (!configContent.Contains("mining_enable_automatic_thread_affinity") || !configContent.Contains("mining_manual_thread_affinity") || !configContent.Contains("mining_enable_cache"))
                    {
                        ClassConsole.WriteLine("Config.json has been updated, mining thread affinity, mining cache settings are implemented, close your solo miner and edit those settings if you want to enable them.", 3);
                        WriteMinerConfig();
                    }
                    
                    if (ClassMinerConfigObject.mining_enable_cache)
                    {
                        InitializeMiningCache();
                    }
                    return true;
                }
                else
                {
                    if (!configContent.Contains("mining_enable_automatic_thread_affinity") || !configContent.Contains("mining_manual_thread_affinity") || !configContent.Contains("mining_enable_cache"))
                    {
                        ClassConsole.WriteLine("Config.json has been updated, mining thread affinity, mining cache settings are implemented, close your solo miner and edit those settings if you want to enable them.", 3);
                        WriteMinerConfig();
                    }

                    if (ClassMinerConfigObject.mining_enable_cache)
                    {
                        InitializeMiningCache();
                    }
                    return true;
                }


            }
            return false;
        }

        /// <summary>
        /// Initialize mining cache.
        /// </summary>
        private static void InitializeMiningCache()
        {
            ClassConsole.WriteLine("Be carefull, the mining cache feature is in beta and can use a lot of RAM, this function is not tested at 100% and need more features for probably provide more luck on mining.", 3);

            for (int i = 0; i < ClassMinerConfigObject.mining_thread; i++)
            {
                try
                {
                    if (!DictionaryCacheMining.ContainsKey(i))
                    {
                        DictionaryCacheMining.Add(i, new Dictionary<string, string>());
                    }
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Force to close the process of the program by CTRL+C
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Console.WriteLine("Closing miner.");
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Write miner config file.
        /// </summary>
        private static void WriteMinerConfig()
        {

            #region old config file
            /*
            Console.WriteLine("Write config.ini file..");
            File.Create(GetCurrentPathConfig(OldConfigFile)).Close();
            using (StreamWriter writeConfig = new StreamWriter(GetCurrentPathConfig(OldConfigFile))
            {
                AutoFlush = true
            })
            {
                writeConfig.WriteLine("WALLET_ADDRESS=" + ClassMinerConfigObject.mining_wallet_address);
                writeConfig.WriteLine("MINING_THREAD=" + ClassMinerConfigObject.mining_thread);
                writeConfig.WriteLine("MINING_THREAD_PRIORITY=" + ClassMinerConfigObject.mining_thread_priority);

                if (ShareJobRange)
                {
                    writeConfig.WriteLine("MINING_THREAD_SPREAD_JOB=Y");
                }
                else
                {
                    writeConfig.WriteLine("MINING_THREAD_SPREAD_JOB=N");
                }
                writeConfig.WriteLine("");
                writeConfig.WriteLine("## FOR MINING PROXY ##");
                if (ClassMinerConfigObject.mining_enable_proxy)
                {
                    writeConfig.WriteLine("PROXY_ENABLE=Y");
                    writeConfig.WriteLine("PROXY_PORT=" + ProxyPort);
                    writeConfig.WriteLine("PROXY_HOST=" + ProxyHost);
                    writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_START=" + ClassMinerConfigObject.mining_percent_difficulty_start);
                    writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_END=" + ClassMinerConfigObject.mining_percent_difficulty_end);
                }
                else
                {
                    writeConfig.WriteLine("PROXY_ENABLE=N");
                    writeConfig.WriteLine("PROXY_PORT=0");
                    writeConfig.WriteLine("PROXY_HOST=NONE");
                    writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_START=0");
                    writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_END=0");

                }
            }
            */
            #endregion

            ClassConsole.WriteLine("Write config.json.", 1);
            File.Create(GetCurrentPathConfig(ConfigFile)).Close();
            using (StreamWriter writeConfig = new StreamWriter(GetCurrentPathConfig(ConfigFile))
            {
                AutoFlush = true
            })
            {
                writeConfig.Write(JsonConvert.SerializeObject(ClassMinerConfigObject, Formatting.Indented));
            }
        }

        /// <summary>
        /// Start to connect the miner to the network
        /// </summary>
        private static async Task<bool> StartConnectMinerAsync()
        {
            CertificateConnection = ClassUtils.GenerateCertificate();
            MalformedPacket = string.Empty;
            if (ObjectSeedNodeNetwork == null)
            {
                ObjectSeedNodeNetwork = new ClassSeedNodeConnector();
            }
            else
            {
                ObjectSeedNodeNetwork.DisconnectToSeed();
            }
            if (!ClassMinerConfigObject.mining_enable_proxy)
            {
                while (!await ObjectSeedNodeNetwork.StartConnectToSeedAsync(string.Empty, ClassConnectorSetting.SeedNodePort))
                {
                    ClassConsole.WriteLine("Can't connect to the network, retry in 5 seconds..", 3);
                    Thread.Sleep(ClassConnectorSetting.MaxTimeoutConnect);
                }
            }
            else
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (!await ObjectSeedNodeNetwork.StartConnectToSeedAsync(ClassMinerConfigObject.mining_proxy_host, ClassMinerConfigObject.mining_proxy_port))
                {
                    stopwatch.Restart();
                    ClassConsole.WriteLine("Can't connect to the proxy, retry in 5 seconds..", 3);
                    Thread.Sleep(ClassConnectorSetting.MaxTimeoutConnect);
                }
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 0)
                {
                    PacketSpeedSend = (int)stopwatch.ElapsedMilliseconds;
                }
            }
            if (!ClassMinerConfigObject.mining_enable_proxy)
            {
                ClassConsole.WriteLine("Miner connected to the network, generate certificate connection..", 2);
            }
            if (!ClassMinerConfigObject.mining_enable_proxy)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(CertificateConnection, string.Empty, false, false))
                {
                    IsConnected = false;
                    return false;
                }
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 0)
                {
                    PacketSpeedSend = (int)stopwatch.ElapsedMilliseconds;
                }
            }
            LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (!ClassMinerConfigObject.mining_enable_proxy)
            {
                if (PacketSpeedSend > 0)
                {
                    Thread.Sleep(PacketSpeedSend);
                }
                ClassConsole.WriteLine("Send wallet address for login your solo miner..", 2);
                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassConnectorSettingEnumeration.MinerLoginType + "|" + ClassMinerConfigObject.mining_wallet_address, CertificateConnection, false, true))
                {
                    IsConnected = false;
                    return false;
                }
            }
            else
            {
                if (PacketSpeedSend > 0)
                {
                    Thread.Sleep(PacketSpeedSend);
                }
                ClassConsole.WriteLine("Send wallet address for login your solo miner..", 2);
                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassConnectorSettingEnumeration.MinerLoginType + "|" + ClassMinerConfigObject.mining_wallet_address + "|" + ClassMinerConfigObject.mining_percent_difficulty_end + "|" + ClassMinerConfigObject.mining_percent_difficulty_start + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                {
                    IsConnected = false;
                    return false;
                }
            }
            IsConnected = true;
            ListenNetwork();
            if (!CheckConnectionStarted)
            {
                CheckConnectionStarted = true;
                CheckNetwork();
            }

            return true;
        }

        /// <summary>
        /// Check the connection of the miner to the network.
        /// </summary>
        private static void CheckNetwork()
        {
            if (ThreadCheckNetwork != null && (ThreadCheckNetwork.IsAlive || ThreadCheckNetwork != null))
            {
                ThreadCheckNetwork.Abort();
                GC.SuppressFinalize(ThreadCheckNetwork);
            }
            ThreadCheckNetwork = new Thread(async delegate ()
            {
                ClassConsole.WriteLine("Check connection enabled.", 2);

                Thread.Sleep(ClassConnectorSetting.MaxTimeoutConnect);
                while (true)
                {
                    try
                    {
                        if (!IsConnected || !LoginAccepted || !ObjectSeedNodeNetwork.ReturnStatus() || LastPacketReceived + TimeoutPacketReceived < DateTimeOffset.Now.ToUnixTimeSeconds())
                        {
                            ClassConsole.WriteLine("Miner connection lost or aborted, retry to connect..", 3);
                            StopMining();
                            CurrentBlockId = "";
                            CurrentBlockHash = "";
                            while (!await StartConnectMinerAsync())
                            {
                                ClassConsole.WriteLine("Can't connect to the proxy, retry in 5 seconds..", 3);
                                Thread.Sleep(ClassConnectorSetting.MaxTimeoutConnect);
                            }
                        }
                    }
                    catch
                    {
                        ClassConsole.WriteLine("Miner connection lost or aborted, retry to connect..", 3);
                        StopMining();
                        CurrentBlockId = "";
                        CurrentBlockHash = "";
                        while (!await StartConnectMinerAsync())
                        {
                            ClassConsole.WriteLine("Can't connect to the proxy, retry in 5 seconds..", 3);
                            Thread.Sleep(ClassConnectorSetting.MaxTimeoutConnect);
                        }
                    }

                    Thread.Sleep(ThreadCheckNetworkInterval);
                }
            });
            ThreadCheckNetwork.Start();
        }

        /// <summary>
        /// Force disconnect the miner.
        /// </summary>
        private static void DisconnectNetwork()
        {
            IsConnected = false;
            LoginAccepted = false;
            try
            {
                ObjectSeedNodeNetwork.DisconnectToSeed();
            }
            catch
            {

            }
            StopMining();

        }

        /// <summary>
        /// Listen packet received from blockchain.
        /// </summary>
        private static void ListenNetwork()
        {
            if (ThreadListenNetwork != null && (ThreadListenNetwork.IsAlive || ThreadListenNetwork != null))
            {
                ThreadListenNetwork.Abort();
            }
            ThreadListenNetwork = new Thread(async delegate ()
            {
                while (true)
                {
                    try
                    {
                        if (!ClassMinerConfigObject.mining_enable_proxy)
                        {
                            string packet = await ObjectSeedNodeNetwork.ReceivePacketFromSeedNodeAsync(CertificateConnection, false, true);
                            if (packet == ClassSeedNodeStatus.SeedError)
                            {
                                ClassConsole.WriteLine("Network error received. Waiting network checker..", 3);
                                DisconnectNetwork();
                                break;
                            }
                            if (packet.Contains("*"))
                            {
                                if (MalformedPacket != null)
                                {
                                    if (!string.IsNullOrEmpty(MalformedPacket))
                                    {
                                        packet = MalformedPacket + packet;
                                        MalformedPacket = string.Empty;
                                    }
                                }
                                var splitPacket = packet.Split(new[] { "*" }, StringSplitOptions.None);
                                foreach (var packetEach in splitPacket)
                                {
                                    if (packetEach != null)
                                    {
                                        if (!string.IsNullOrEmpty(packetEach))
                                        {
                                            if (packetEach.Length > 1)
                                            {
                                                var packetRequest = packetEach.Replace("*", "");
                                                if (packetRequest == ClassSeedNodeStatus.SeedError)
                                                {
                                                    ClassConsole.WriteLine("Network error received. Waiting network checker..", 3);
                                                    DisconnectNetwork();
                                                    break;
                                                }
                                                if (packetRequest != ClassSeedNodeStatus.SeedNone && packetRequest != ClassSeedNodeStatus.SeedError)
                                                {
                                                    await HandlePacketMiningAsync(packetRequest);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (MalformedPacket.Length < int.MaxValue - 1 || (long)(MalformedPacket.Length + packet.Length) < int.MaxValue - 1)
                                {
                                    MalformedPacket += packet;
                                }
                                else
                                {
                                    MalformedPacket = string.Empty;
                                }
                            }
                        }
                        else
                        {
                            string packet = await ObjectSeedNodeNetwork.ReceivePacketFromSeedNodeAsync(string.Empty, false, false);
                            if (packet == ClassSeedNodeStatus.SeedError)
                            {
                                ClassConsole.WriteLine("Network error received. Waiting network checker..", 3);
                                DisconnectNetwork();
                                break;
                            }

                            if (packet != ClassSeedNodeStatus.SeedNone)
                            {
                                await HandlePacketMiningAsync(packet);
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine("Listen Network error exception: " + error.Message);
                        DisconnectNetwork();
                        break;
                    }
                }
            });
            ThreadListenNetwork.Start();
        }

        /// <summary>
        /// Handle packet for mining.
        /// </summary>
        /// <param name="packet"></param>
        private static async Task HandlePacketMiningAsync(string packet)
        {
            LastPacketReceived = DateTimeOffset.Now.ToUnixTimeSeconds();
            try
            {
                var splitPacket = packet.Split(new[] { "|" }, StringSplitOptions.None);
                switch (splitPacket[0])
                {
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendLoginAccepted:
                        ClassConsole.WriteLine("Miner login accepted, start to mine..", 1);
                        LoginAccepted = true;
                        MiningProcessingRequest();
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendListBlockMethod:
                        var methodList = splitPacket[1];
                        if (methodList.Contains("#"))
                        {
                            var splitMethodList = methodList.Split(new[] { "#" }, StringSplitOptions.None);
                            if (ListeMiningMethodName.Count > 1)
                            {
                                foreach (var methodName in splitMethodList)
                                {
                                    if (!string.IsNullOrEmpty(methodName))
                                    {
                                        if (ListeMiningMethodName.Contains(methodName) == false)
                                        {
                                            ListeMiningMethodName.Add(methodName);
                                        }
                                        if (!ClassMinerConfigObject.mining_enable_proxy)
                                        {
                                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod + "|" + methodName, CertificateConnection, false, true))
                                            {
                                                DisconnectNetwork();
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod + "|" + methodName, string.Empty, false, false))
                                            {
                                                DisconnectNetwork();
                                                break;
                                            }
                                        }
                                        await Task.Delay(1000);
                                    }
                                }
                            }
                            else
                            {

                                foreach (var methodName in splitMethodList)
                                {
                                    if (!string.IsNullOrEmpty(methodName))
                                    {
                                        if (ListeMiningMethodName.Contains(methodName) == false)
                                        {
                                            ListeMiningMethodName.Add(methodName);
                                        }
                                        if (!ClassMinerConfigObject.mining_enable_proxy)
                                        {
                                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod + "|" + methodName, CertificateConnection, false, true))
                                            {
                                                DisconnectNetwork();
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod + "|" + methodName, string.Empty, false, false))
                                            {
                                                DisconnectNetwork();
                                                break;
                                            }
                                        }
                                        await Task.Delay(1000);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (ListeMiningMethodName.Contains(methodList) == false)
                            {
                                ListeMiningMethodName.Add(methodList);
                            }
                            if (!ClassMinerConfigObject.mining_enable_proxy)
                            {
                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod + "|" + methodList, CertificateConnection, false, true))
                                {
                                    DisconnectNetwork();
                                    break;
                                }
                            }
                            else
                            {
                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod + "|" + methodList, string.Empty, false, false))
                                {
                                    DisconnectNetwork();
                                    break;
                                }
                            }
                        }
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendContentBlockMethod:
                        if (ListeMiningMethodContent.Count == 0)
                        {
                            ListeMiningMethodContent.Add(splitPacket[1]);
                        }
                        else
                        {
                            ListeMiningMethodContent[0] = splitPacket[1];
                        }
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendCurrentBlockMining:

                        CalculateHashrate();
                        bool proxy = false;
                        var splitBlockContent = splitPacket[1].Split(new[] { "&" }, StringSplitOptions.None);
                        if (ClassMinerConfigObject.mining_enable_proxy)
                        {
                            if (splitBlockContent[11] == "PROXY=YES")
                            {
                                proxy = true;
                            }
                        }
                        if (CurrentBlockId != splitBlockContent[0].Replace("ID=", "") || CurrentBlockHash != splitBlockContent[1].Replace("HASH=", "") || proxy)
                        {

                            if (!ClassMinerConfigObject.mining_enable_proxy)
                            {
                                ClassConsole.WriteLine("New block to mine " + splitBlockContent[0], 2);
                            }
                            else
                            {
                                ClassConsole.WriteLine("Current block to mine " + splitBlockContent[0], 2);
                            }

                            try
                            {
                                if (CurrentBlockId == splitBlockContent[0].Replace("ID=", ""))
                                {
                                    ClassConsole.WriteLine(
                                        "Current Block ID: " + CurrentBlockId + " has been renewed.", 3);
                                }
                                CurrentBlockId = splitBlockContent[0].Replace("ID=", "");
                                CurrentBlockHash = splitBlockContent[1].Replace("HASH=", "");
                                CurrentBlockAlgorithm = splitBlockContent[2].Replace("ALGORITHM=", "");
                                CurrentBlockSize = splitBlockContent[3].Replace("SIZE=", "");
                                CurrentBlockMethod = splitBlockContent[4].Replace("METHOD=", "");
                                CurrentBlockKey = splitBlockContent[5].Replace("KEY=", "");
                                CurrentBlockJob = splitBlockContent[6].Replace("JOB=", "");
                                CurrentBlockReward = splitBlockContent[7].Replace("REWARD=", "");
                                CurrentBlockDifficulty = splitBlockContent[8].Replace("DIFFICULTY=", "");
                                CurrentBlockTimestampCreate = splitBlockContent[9].Replace("TIMESTAMP=", "");
                                CurrentBlockIndication = splitBlockContent[10].Replace("INDICATION=", "");
                                StopMining();
                                if (ClassMinerConfigObject.mining_enable_cache)
                                {
                                    ClearMiningCache();
                                }
                                CanMining = true;
                                var splitCurrentBlockJob = CurrentBlockJob.Split(new[] { ";" }, StringSplitOptions.None);
                                var minRange = decimal.Parse(splitCurrentBlockJob[0]);
                                var maxRange = decimal.Parse(splitCurrentBlockJob[1]);

                                if (ClassMinerConfigObject.mining_enable_proxy)
                                {
                                    ClassConsole.WriteLine("Job range received from proxy: " + minRange + "|" + maxRange + "", 2);
                                }
                                var minProxyStart = maxRange - minRange;
                                var incrementProxyRange = minProxyStart / ClassMinerConfigObject.mining_thread;
                                var previousProxyMaxRange = minRange;

                                int idMethod = 0;
                                if (ListeMiningMethodName.Count >= 1)
                                {
                                    for (int i = 0; i < ListeMiningMethodName.Count; i++)
                                    {
                                        if (i < ListeMiningMethodName.Count)
                                        {
                                            if (ListeMiningMethodName[i] == CurrentBlockMethod)
                                            {
                                                idMethod = i;
                                            }
                                        }
                                    }
                                }

                                var splitMethod = ListeMiningMethodContent[idMethod].Split(new[] { "#" }, StringSplitOptions.None);

                                CurrentRoundAesRound = int.Parse(splitMethod[0]);
                                CurrentRoundAesSize = int.Parse(splitMethod[1]);
                                CurrentRoundAesKey = splitMethod[2];
                                CurrentRoundXorKey = int.Parse(splitMethod[3]);

                                using (var pdb = new PasswordDeriveBytes(CurrentBlockKey, Encoding.UTF8.GetBytes(CurrentRoundAesKey)))
                                {
                                    CurrentAesKeyBytes = pdb.GetBytes(CurrentRoundAesSize / 8);
                                    CurrentAesIvBytes = pdb.GetBytes(CurrentRoundAesSize / 8);
                                }

                                int totalThreadExecuted = 0;
                                for (int i = 0; i < ClassMinerConfigObject.mining_thread; i++)
                                {
                                    if (i < ClassMinerConfigObject.mining_thread)
                                    {
                                        int i1 = i + 1;
                                        int iThread = i;
                                        ThreadMining[i] = new Thread(() => InitializeMiningThread(iThread))
                                        {
                                            IsBackground = false
                                        };
                                        ThreadMining[i].SetApartmentState(ApartmentState.MTA);
                                        ThreadMining[i].Start();
                                        totalThreadExecuted++;
                                    }
                                }
                            }
                            catch
                            {
                                ClassConsole.WriteLine("Block template not completly received, stop mining and ask again the blocktemplate", 2);
                                StopMining();
                            }
                        }
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendJobStatus:
                        switch (splitPacket[1])
                        {
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareUnlock:
                                TotalBlockAccepted++;
                                ClassConsole.WriteLine("Block accepted, stop mining, wait new block.", 1);
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareWrong:
                                TotalBlockRefused++;
                                ClassConsole.WriteLine("Block not accepted, stop mining, wait new block.", 3);
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareAleady:
                                if (CurrentBlockId == splitPacket[1])
                                {
                                    ClassConsole.WriteLine(splitPacket[1] + " Orphaned, someone already got it, stop mining, wait new block.", 3);
                                }
                                else
                                {
                                    ClassConsole.WriteLine(splitPacket[1] + " Orphaned, someone already get it.", 3);
                                }
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareNotExist:
                                ClassConsole.WriteLine("Block mined does not exist, stop mining, wait new block.", 4);
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareGood:
                                TotalShareAccepted++;
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareBad:
                                ClassConsole.WriteLine("Block not accepted, someone already got it or your share is invalid.", 3);
                                TotalShareInvalid++;
                                break;
                        }

                        break;
                }
            }
            catch
            {
                DisconnectNetwork();
            }

        }

        /// <summary>
        /// Ask new mining method and current blocktemplate automaticaly.
        /// </summary>
        private static void MiningProcessingRequest()
        {
            bool error = true;
            while (error)
            {
                try
                {
                    if (ThreadMiningMethod != null && (ThreadMiningMethod.IsAlive || ThreadMiningMethod != null))
                    {
                        ThreadMiningMethod.Abort();
                        GC.SuppressFinalize(ThreadMiningMethod);
                    }
                    error = false;
                }
                catch
                {
                    error = true;
                }
            }

            ThreadMiningMethod = new Thread(async delegate ()
            {
                if (!ClassMinerConfigObject.mining_enable_proxy)
                {
                    while (IsConnected)
                    {

                        if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskListBlockMethod, CertificateConnection, false, true))
                        {
                            DisconnectNetwork();
                            break;
                        }
                        Thread.Sleep(1000);
                        if (ListeMiningMethodContent.Count > 0)
                        {
                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, CertificateConnection, false, true))
                            {
                                DisconnectNetwork();
                                break;
                            }
                        }
                        Thread.Sleep(1000);
                    }
                }
                else
                {

                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskListBlockMethod, string.Empty, false, false))
                    {
                        DisconnectNetwork();
                    }
                    Thread.Sleep(1000);

                    if (ListeMiningMethodContent.Count > 0)
                    {
                        if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, string.Empty, false, false))
                        {
                            DisconnectNetwork();
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
            ThreadMiningMethod.Start();
        }

        /// <summary>
        /// Clear Mining Cache
        /// </summary>
        private static void ClearMiningCache()
        {
            bool error = true;
            while (error)
            {
                try
                {
                    if (!ClassMinerConfigObject.mining_thread_spread_job)
                    {
                        ClassConsole.WriteLine("Clear mining cache | total calculation cached: " + DictionaryCacheMining[0].Count.ToString("F0"), 5);
                        DictionaryCacheMining[0].Clear();
                        DictionaryCacheMining[0] = new Dictionary<string, string>();
                    }
                    else
                    {
                        var listKey = DictionaryCacheMining.Keys.ToList();
                        for (int i = 0; i < listKey.Count; i++)
                        {
                            if (i < listKey.Count)
                            {
                                ClassConsole.WriteLine("Clear mining cache from thread id: " + listKey[i] + " | Total calculation cached: " + DictionaryCacheMining[listKey[i]].Count.ToString("F0"), 5);
                                DictionaryCacheMining[listKey[i]].Clear();
                                DictionaryCacheMining[listKey[i]] = new Dictionary<string, string>();
                            }
                        }
                    }
                    error = false;
                }
                catch
                {
                    error = true;
                }
            }
            GC.Collect();
        }

        /// <summary>
        /// Stop mining.
        /// </summary>
        private static void StopMining()
        {
            CanMining = false;
            try
            {
                if (cts != null)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                }
            }
            catch
            {

            }
            for (int i = 0; i < ThreadMining.Length; i++)
            {
                if (i < ThreadMining.Length)
                {
                    if (ThreadMining[i] != null)
                    {
                        bool error = true;
                        while (error)
                        {
                            try
                            {
                                if (ThreadMining[i] != null && (ThreadMining[i].IsAlive || ThreadMining[i] != null))
                                {

                                    ThreadMining[i].Abort();
                                    ThreadMining[i] = null;
                                }
                                error = false;
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
            cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Initialization of the mining thread executed.
        /// </summary>
        /// <param name="iThread"></param>
        private static void InitializeMiningThread(int iThread)
        {
            if (ClassMinerConfigObject.mining_enable_automatic_thread_affinity && string.IsNullOrEmpty(ClassMinerConfigObject.mining_manual_thread_affinity))
            {
                ClassUtilityAffinity.SetAffinity(iThread);
            }
            else
            {
                if (!string.IsNullOrEmpty(ClassMinerConfigObject.mining_manual_thread_affinity))
                {
                    ClassUtilityAffinity.SetManualAffinity(ClassMinerConfigObject.mining_manual_thread_affinity);
                }
            }
            int i1 = iThread + 1;
            var splitCurrentBlockJob = CurrentBlockJob.Split(new[] { ";" }, StringSplitOptions.None);
            var minRange = decimal.Parse(splitCurrentBlockJob[0]);
            var maxRange = decimal.Parse(splitCurrentBlockJob[1]);

            var minProxyStart = maxRange - minRange;
            var incrementProxyRange = minProxyStart / ClassMinerConfigObject.mining_thread;
            var previousProxyMaxRange = minRange;

            switch (ClassMinerConfigObject.mining_thread_priority)
            {
                case 0:
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

                    break;
                case 1:
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                    break;
                case 2:
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                    break;
                case 3:
                    Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                    break;
                case 4:
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                    break;
            }

            if (ClassMinerConfigObject.mining_thread_spread_job)
            {
                if (ClassMinerConfigObject.mining_enable_proxy)
                {
                    if (minRange > 0)
                    {
                        decimal minRangeTmp = previousProxyMaxRange;
                        decimal maxRangeTmp = minRangeTmp + incrementProxyRange;
                        previousProxyMaxRange = maxRangeTmp;
                        StartMiningAsync(iThread, Math.Round(minRangeTmp, 0), (Math.Round(maxRangeTmp, 0)));
                    }
                    else
                    {

                        decimal minRangeTmp = Math.Round((maxRange / ClassMinerConfigObject.mining_thread) * (i1 - 1), 0);
                        decimal maxRangeTmp = (Math.Round(((maxRange / ClassMinerConfigObject.mining_thread) * i1), 0));
                        StartMiningAsync(iThread, minRangeTmp, maxRangeTmp);
                    }
                }
                else
                {
                    decimal minRangeTmp = Math.Round((maxRange / ClassMinerConfigObject.mining_thread) * (i1 - 1), 0);
                    decimal maxRangeTmp = (Math.Round(((maxRange / ClassMinerConfigObject.mining_thread) * i1), 0));
                    StartMiningAsync(iThread, minRangeTmp, maxRangeTmp);
                }

            }
            else
            {
                StartMiningAsync(iThread, minRange, maxRange);
            }
        }


        /// <summary>
        /// Start mining.
        /// </summary>
        /// <param name="idThread"></param>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        private static void StartMiningAsync(int idThread, decimal minRange, decimal maxRange)
        {

            if (minRange <= 1)
            {
                minRange = 2;
            }

            while (ListeMiningMethodName.Count == 0)
            {
                ClassConsole.WriteLine("No method content received, waiting to receive them before..", 2);
                Thread.Sleep(1000);
            }

            var currentBlockId = CurrentBlockId;
            var currentBlockTimestamp = CurrentBlockTimestampCreate;

            var currentBlockDifficulty = decimal.Parse(CurrentBlockDifficulty);

            var currentBlockDifficultyLength = currentBlockDifficulty.ToString("F0").Length;

            ClassConsole.WriteLine("Thread: " + idThread + " min range:" + minRange + " max range:" + maxRange + " | Host target: " + ObjectSeedNodeNetwork.ReturnCurrentSeedNodeHost(), 1);

            ClassConsole.WriteLine("Current Mining Method: " + CurrentBlockMethod + " = AES ROUND: " + CurrentRoundAesRound + " AES SIZE: " + CurrentRoundAesSize + " AES BYTE KEY: " + CurrentRoundAesKey + " XOR KEY: " + CurrentRoundXorKey, 1);

            var currentMaxRangeLength = maxRange.ToString("F0").Length;

            while (CanMining)
            {
                if (!GetCancellationMiningTaskStatus())
                {
                    try
                    {
                        if (CurrentBlockId != currentBlockId || currentBlockTimestamp != CurrentBlockTimestampCreate)
                        {
                            currentBlockId = CurrentBlockId;
                            currentBlockTimestamp = CurrentBlockTimestampCreate;
                            currentBlockDifficulty = decimal.Parse(CurrentBlockDifficulty);
                            currentBlockDifficultyLength = currentBlockDifficulty.ToString("F0").Length;
                            if (ClassMinerConfigObject.mining_enable_cache)
                            {
                                ClearMiningCache();
                            }
                        }
                        if (ClassMinerConfigObject.mining_enable_cache)
                        {
                            var idCache = idThread;
                            if (!ClassMinerConfigObject.mining_thread_spread_job)
                            {
                                idCache = 0;
                            }
                            if (DictionaryCacheMining[idCache].Count >= int.MaxValue - 1)
                            {
                                MiningComputeProcess(idThread, minRange, maxRange, currentBlockDifficulty, currentBlockDifficultyLength, currentMaxRangeLength, true);
                            }
                            else
                            {
                                MiningComputeProcess(idThread, minRange, maxRange, currentBlockDifficulty, currentBlockDifficultyLength, currentMaxRangeLength);
                            }
                        }
                        else
                        {
                            MiningComputeProcess(idThread, minRange, maxRange, currentBlockDifficulty, currentBlockDifficultyLength, currentMaxRangeLength);

                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        private static void MiningComputeProcess(int idThread, decimal minRange, decimal maxRange, decimal currentBlockDifficulty, int currentBlockDifficultyLength, int currentMaxRangeLength, bool cacheIsFull = false)
        {

            string firstNumber = "0";
            string secondNumber = "0";

            while (CanMining)
            {
                if (ClassUtility.GetRandom() >= ClassUtility.GetRandom())
                {
                    firstNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);
                }
                else
                {
                    firstNumber = ClassUtility.GetRandomBetweenJob(minRange, maxRange).ToString("F0");
                }
                decimal tmpFirstNumber = decimal.Parse(firstNumber);
                if (tmpFirstNumber >= 2 && tmpFirstNumber <= currentBlockDifficulty)
                {
                    break;
                }
            }

            while (CanMining)
            {
                if (ClassUtility.GetRandom() >= ClassUtility.GetRandom())
                {

                    secondNumber = ClassUtility.GenerateNumberMathCalculation(minRange, maxRange);
                }
                else
                {
                    secondNumber = ClassUtility.GetRandomBetweenJob(minRange, maxRange).ToString("F0");
                }
                decimal tmpSecondNumber = decimal.Parse(secondNumber);
                if (tmpSecondNumber >= 2 && tmpSecondNumber <= currentBlockDifficulty)
                {
                    break;
                }
            }

            if (ClassMinerConfigObject.mining_enable_cache && !cacheIsFull)
            {
                var idCache = idThread;
                if (!ClassMinerConfigObject.mining_thread_spread_job)
                {
                    idCache = 0;
                }
                if (!DictionaryCacheMining[idCache].ContainsKey(firstNumber + secondNumber))
                {
                    DictionaryCacheMining[idCache].Add(firstNumber + secondNumber, string.Empty);

                    for (int k = 0; k < ClassUtility.randomOperatorCalculation.Length; k++)
                    {
                        if (k < ClassUtility.randomOperatorCalculation.Length)
                        {
                            //Console.WriteLine("Calculation generated by Thread " + idThread + " = " + firstNumber + " " + ClassUtility.randomOperatorCalculation[k] + " " + secondNumber);
                            string calcul = firstNumber + " " + ClassUtility.randomOperatorCalculation[k] + " " + secondNumber;
                            decimal calculCompute = ClassUtility.ComputeCalculation(firstNumber, ClassUtility.randomOperatorCalculation[k], secondNumber);

                            if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it. 
                            {
                                if (calculCompute >= 2 && calculCompute <= currentBlockDifficulty)
                                {
                                    string encryptedShare = calcul;

                                    encryptedShare = MakeEncryptedShare(encryptedShare, idThread);

                                    if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                    {
                                        string hashShare = ClassUtility.GenerateSHA512(encryptedShare);

                                        TotalMiningHashrateRound[idThread]++;
                                        if (!CanMining)
                                        {
                                            return;
                                        }

                                        if (hashShare == CurrentBlockIndication)
                                        {
                                            Task.Factory.StartNew(async delegate ()
                                            {
                                                ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                                if (!ClassMinerConfigObject.mining_enable_proxy)
                                                {
                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                        ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                        calculCompute.ToString("F0") +
                                                        "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                    {
                                                        DisconnectNetwork();
                                                    }
                                                }
                                                else
                                                {
                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                     ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                     calculCompute.ToString("F0") +
                                                     "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                                    {
                                                        DisconnectNetwork();
                                                    }
                                                }
                                            }).ConfigureAwait(false);
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
                if (!DictionaryCacheMining[idCache].ContainsKey(secondNumber + firstNumber))
                {
                    DictionaryCacheMining[idCache].Add(secondNumber + firstNumber, string.Empty);

                    // Test reverted
                    for (int k = 0; k < ClassUtility.randomOperatorCalculation.Length; k++)
                    {
                        if (k < ClassUtility.randomOperatorCalculation.Length)
                        {
                            string calcul = secondNumber + " " + ClassUtility.randomOperatorCalculation[k] + " " + firstNumber;

                            decimal calculCompute = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.randomOperatorCalculation[k], firstNumber);
                            if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it. 
                            {
                                if (calculCompute >= 2 && calculCompute <= currentBlockDifficulty)
                                {
                                    string encryptedShare = calcul;

                                    encryptedShare = MakeEncryptedShare(encryptedShare, idThread);
                                    if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                    {
                                        string hashShare = ClassUtility.GenerateSHA512(encryptedShare);

                                        TotalMiningHashrateRound[idThread]++;
                                        if (!CanMining)
                                        {
                                            return;
                                        }

                                        if (hashShare == CurrentBlockIndication)
                                        {
                                            Task.Factory.StartNew(async delegate ()
                                            {
                                                ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                                if (!ClassMinerConfigObject.mining_enable_proxy)
                                                {
                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                        ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                        calculCompute.ToString("F0") +
                                                        "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                    {
                                                        DisconnectNetwork();
                                                    }
                                                }
                                                else
                                                {
                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                     ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                     calculCompute.ToString("F0") +
                                                     "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                                    {
                                                        DisconnectNetwork();
                                                    }
                                                }
                                            }).ConfigureAwait(false);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int k = 0; k < ClassUtility.randomOperatorCalculation.Length; k++)
                {
                    if (k < ClassUtility.randomOperatorCalculation.Length)
                    {
                        string calcul = firstNumber + " " + ClassUtility.randomOperatorCalculation[k] + " " + secondNumber;
                        decimal calculCompute = ClassUtility.ComputeCalculation(firstNumber, ClassUtility.randomOperatorCalculation[k], secondNumber);

                        string calculComputeString = calculCompute.ToString();
                        if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it. 
                        {
                            if (calculCompute >= 2 && calculCompute <= currentBlockDifficulty)
                            {
                                string encryptedShare = calcul;

                                encryptedShare = MakeEncryptedShare(encryptedShare, idThread);

                                if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                {
                                    string hashShare = ClassUtility.GenerateSHA512(encryptedShare);

                                    TotalMiningHashrateRound[idThread]++;
                                    if (!CanMining)
                                    {
                                        return;
                                    }

                                    if (hashShare == CurrentBlockIndication)
                                    {
                                        Task.Factory.StartNew(async delegate ()
                                        {
                                            ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                            if (!ClassMinerConfigObject.mining_enable_proxy)
                                            {
                                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                    ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                    calculCompute.ToString("F0") +
                                                    "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                {
                                                    DisconnectNetwork();
                                                }
                                            }
                                            else
                                            {
                                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                 ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                 calculCompute.ToString("F0") +
                                                 "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                                {
                                                    DisconnectNetwork();
                                                }
                                            }
                                            StopMining();
                                        }).ConfigureAwait(false);
                                        break;
                                    }
                                }
                            }
                        }

                        // Test reverted
                        calcul = secondNumber + " " + ClassUtility.randomOperatorCalculation[k] + " " + firstNumber;

                        calculCompute = ClassUtility.ComputeCalculation(secondNumber, ClassUtility.randomOperatorCalculation[k], firstNumber);
                        calculComputeString = calculCompute.ToString();
                        if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it. 
                        {
                            if (calculCompute >= 2 && calculCompute <= currentBlockDifficulty)
                            {
                                string encryptedShare = calcul;

                                encryptedShare = MakeEncryptedShare(encryptedShare, idThread);
                                if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                {
                                    string hashShare = ClassUtility.GenerateSHA512(encryptedShare);

                                    TotalMiningHashrateRound[idThread]++;
                                    if (!CanMining)
                                    {
                                        return;
                                    }

                                    if (hashShare == CurrentBlockIndication)
                                    {
                                        Task.Factory.StartNew(async delegate ()
                                        {
                                            ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                            if (!ClassMinerConfigObject.mining_enable_proxy)
                                            {
                                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                    ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                    calculCompute.ToString("F0") +
                                                    "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                {
                                                    DisconnectNetwork();
                                                }
                                            }
                                            else
                                            {
                                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                 ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                 calculCompute.ToString("F0") +
                                                 "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                                {
                                                    DisconnectNetwork();
                                                }
                                            }
                                            StopMining();
                                        }).ConfigureAwait(false);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }



        private static bool GetCancellationMiningTaskStatus()
        {
            try
            {
                if (cts.IsCancellationRequested)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Encrypt math calculation with the current mining method
        /// </summary>
        /// <param name="calculation"></param>
        /// <returns></returns>
        private static string MakeEncryptedShare(string calculation, int idThread)
        {

            string encryptedShare = ClassUtility.StringToHex(calculation + CurrentBlockTimestampCreate);

            // Static XOR Encryption -> Key updated from the current mining method.
            encryptedShare = ClassAlgo.EncryptXorShare(encryptedShare, CurrentRoundXorKey.ToString());

            // Dynamic AES Encryption -> Size and Key's from the current mining method and the current block key encryption.
            for (int i = 0; i < CurrentRoundAesRound; i++)
            {
                encryptedShare = ClassAlgo.EncryptAesShare(encryptedShare, CurrentAesKeyBytes, CurrentAesIvBytes, CurrentRoundAesSize);
            }

            // Static XOR Encryption -> Key from the current mining method
            encryptedShare = ClassAlgo.EncryptXorShare(encryptedShare, CurrentRoundXorKey.ToString());

            // Static AES Encryption -> Size and Key's from the current mining method.
            encryptedShare = ClassAlgo.EncryptAesShare(encryptedShare, CurrentAesKeyBytes, CurrentAesIvBytes, CurrentRoundAesSize);

            // Generate SHA512 HASH for the share and return it.
            return ClassUtility.GenerateSHA512(encryptedShare);

        }

        /// <summary>
        /// Calculate the hashrate of the solo miner.
        /// </summary>
        private static void CalculateHashrate()
        {
            if (!CalculateHashrateEnabled)
            {
                CalculateHashrateEnabled = true;
                Task.Factory.StartNew(async () =>
                {
                    var counterTime = 0;
                    while (true)
                    {
                        try
                        {

                            float totalRoundHashrate = 0;


                            for (int i = 0; i < TotalMiningHashrateRound.Count; i++)
                            {
                                if (i < TotalMiningHashrateRound.Count)
                                {
                                    totalRoundHashrate += TotalMiningHashrateRound[i];
                                    if (counterTime >= HashrateIntervalCalculation && CanMining)
                                    {
                                        ClassConsole.WriteLine("Encryption Speed Thread " + i + " : " + (TotalMiningHashrateRound[i]) + " H/s", 4);
                                    }
                                }
                            }


                            TotalHashrate = totalRoundHashrate;

                            for (int i = 0; i < TotalMiningHashrateRound.Count; i++)
                            {
                                if (i < TotalMiningHashrateRound.Count)
                                {
                                    TotalMiningHashrateRound[i] = 0;
                                }
                            }
                            if (CanMining)
                            {
                                if (counterTime == HashrateIntervalCalculation)
                                {
                                    ClassConsole.WriteLine(TotalHashrate + " H/s > UNLOCKED[" + TotalBlockAccepted + "] REFUSED[" + TotalBlockRefused + "]", 4);
                                }
                                if (counterTime < HashrateIntervalCalculation)
                                {
                                    counterTime++;
                                }
                                else
                                {
                                    counterTime = 0;
                                }
                                if (ClassMinerConfigObject.mining_enable_proxy) // Share hashrate information to the proxy solo miner.
                                {
                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ShareHashrate + "|" + TotalHashrate, string.Empty, false, false))
                                    {
                                        DisconnectNetwork();
                                    }
                                }
                            }

                        }
                        catch
                        {
                        }
                        await Task.Delay(1000);
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get current path of the miner.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentPathConfig(string configFile)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + configFile;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                path = path.Replace("\\", "/");
            }
            return path;
        }

        /// <summary>
        /// Get current path of the miner.
        /// </summary>
        /// <returns></returns>
        public static string ConvertPath(string path)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                path = path.Replace("\\", "/");
            }
            return path;
        }
    }
}
