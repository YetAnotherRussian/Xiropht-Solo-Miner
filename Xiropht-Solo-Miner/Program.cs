using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private static bool CanMining;
        private static bool ShareJobRange;
        private static bool LoginAccepted;
        private static bool _checkNetworkEnabled;
        public static int TotalShareAccepted;
        public static int TotalShareInvalid;
        public static long LastPacketReceived;
        private const int TimeoutPacketReceived = 60; // Max 60 seconds.
        private static int PacketSpeedSend;


        /// <summary>
        /// For Proxy.
        /// </summary>
        public static bool UseProxy;
        public static bool ProxyWantShare;
        private static string ProxyHost;
        private static int ProxyPort;
        private static int MiningPercentDifficultyEnd;
        private static int MiningPercentDifficultyStart;

        /// <summary>
        /// Threading.
        /// </summary>
        private const int ThreadCheckNetworkInterval = 1 * 1000; // Check each 5 seconds.
        private static int TotalThreadMining;
        private static Thread ThreadListenNetwork;
        private static Thread ThreadCheckNetwork;
        private static Thread ThreadMiningMethod;
        private static Thread[] ThreadMining;
        private static CancellationTokenSource cts;
        public static List<int> TotalMiningHashrateRound = new List<int>();
        public static float TotalHashrate;
        public static int ThreadMiningPriority;
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
        private static string WalletAddress;
        private static bool CalculateHashrateEnabled;
        private static bool IsLinux;
        private const int TotalConfigLine = 9;
        private static string MalformedPacket;
        
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
            ClassConsole.WriteLine("Xiropht Solo Miner - " + Assembly.GetExecutingAssembly().GetName().Version + "R", 4);

            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                IsLinux = true;
            }
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
            ThreadMiningPriority = 2; // By Default
            MiningPercentDifficultyEnd = 0; // By Default
            MiningPercentDifficultyStart = 0; // By Default


            if (File.Exists(GetCurrentPathConfig()))
            {
                if (LoadConfig())
                {
                    ThreadMining = new Thread[TotalThreadMining];
                    TotalMiningHashrateRound = new List<int>();
                    for (int i = 0; i < TotalThreadMining; i++)
                    {
                        if (i < TotalThreadMining)
                        {
                            TotalMiningHashrateRound.Add(0);
                        }
                    }

                    Console.WriteLine("Connecting to the network..");
                    new Thread(async () => await StartConnectMinerAsync()).Start();
                }
                else
                {
                    ClassConsole.WriteLine("config.ini file invalid, do you want to follow instructions to setting again your config.ini file ? [Y/N]", 2);
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
            ClassConsole.WriteLine("Do you want to use a proxy instead seed node? [Y/N]", 2);
            var choose = Console.ReadLine();

            if (choose.ToLower() == "y")
            {
                Console.WriteLine("Please, write your wallet address or a worker name to start your solo mining: ");
                WalletAddress = Console.ReadLine();

                Console.WriteLine("Write the IP/HOST of your mining proxy: ");
                ProxyHost = Console.ReadLine();
                Console.WriteLine("Write the port of your mining proxy: ");

                while (!int.TryParse(Console.ReadLine(), out ProxyPort))
                {
                    Console.WriteLine("This is not a port number, please try again: ");
                }
                Console.WriteLine("Do you want select a mining range percentage of difficulty? [Y/N]");
                choose = Console.ReadLine();
                if (choose.ToLower() == "y")
                {
                    Console.WriteLine("Select the start percentage range of difficulty [0 to 100]:");
                    while (!int.TryParse(Console.ReadLine(), out MiningPercentDifficultyStart))
                    {
                        Console.WriteLine("This is not a number, please try again: ");
                    }
                    if (MiningPercentDifficultyStart > 100)
                    {
                        MiningPercentDifficultyStart = 100;
                    }
                    if (MiningPercentDifficultyStart < 0)
                    {
                        MiningPercentDifficultyStart = 0;
                    }

                    Console.WriteLine("Select the end percentage range of difficulty [" + MiningPercentDifficultyStart + " to 100]: ");
                    while (!int.TryParse(Console.ReadLine(), out MiningPercentDifficultyEnd))
                    {
                        Console.WriteLine("This is not a number, please try again: ");
                    }
                    if (MiningPercentDifficultyEnd < 1)
                    {
                        MiningPercentDifficultyEnd = 1;
                    }
                    else if (MiningPercentDifficultyEnd > 100)
                    {
                        MiningPercentDifficultyEnd = 100;
                    }

                    if (MiningPercentDifficultyStart > MiningPercentDifficultyEnd)
                    {
                        MiningPercentDifficultyStart -= (MiningPercentDifficultyStart - MiningPercentDifficultyEnd);
                    }
                    else
                    {
                        if (MiningPercentDifficultyStart == MiningPercentDifficultyEnd)
                        {
                            MiningPercentDifficultyStart--;
                        }
                    }
                    if (MiningPercentDifficultyEnd < MiningPercentDifficultyStart)
                    {
                        var tmpPercentStart = MiningPercentDifficultyStart;
                        MiningPercentDifficultyStart = MiningPercentDifficultyEnd;
                        MiningPercentDifficultyEnd = tmpPercentStart;
                    }
                }

                UseProxy = true;
            }
            else
            {

                Console.WriteLine("Please, write your wallet address to start your solo mining: ");
                WalletAddress = Console.ReadLine();
                WalletAddress = ClassUtils.RemoveSpecialCharacters(WalletAddress);

                ClassConsole.WriteLine("Checking wallet address..", 4);
                bool checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(WalletAddress).Result;

                while (WalletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || WalletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize || !checkWalletAddress)
                {
                    Console.WriteLine("Invalid wallet address - Please, write your wallet address to start your solo mining: ");
                    WalletAddress = Console.ReadLine();
                    WalletAddress = ClassUtils.RemoveSpecialCharacters(WalletAddress);
                    ClassConsole.WriteLine("Checking wallet address..", 4);
                    checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(WalletAddress).Result;
                }
                if (checkWalletAddress)
                {
                    ClassConsole.WriteLine("Wallet address: " + WalletAddress + " is valid.", 1);
                }
            }
            Console.WriteLine("How many threads do you want to run? Number of cores detected: " + Environment.ProcessorCount);

            var tmp = Console.ReadLine();
            if (!int.TryParse(tmp, out TotalThreadMining))
            {
                TotalThreadMining = Environment.ProcessorCount;
            }
            ThreadMining = new Thread[TotalThreadMining];


            Console.WriteLine("Do you want share job range per thread ? [Y/N]");
            choose = Console.ReadLine();
            if (choose.ToLower() == "y")
            {
                ShareJobRange = true;
            }
            for (int i = 0; i < TotalThreadMining; i++)
            {
                if (i < TotalThreadMining)
                {
                    TotalMiningHashrateRound.Add(0);
                }
            }

            Console.WriteLine("Select thread priority: 0 = Lowest, 1 = BelowNormal, 2 = Normal, 3 = AboveNormal, 4 = Highest [Default: 2]:");

            if (!int.TryParse(Console.ReadLine(), out ThreadMiningPriority))
            {
                ThreadMiningPriority = 2;
            }


            WriteMinerConfig();

            Console.WriteLine("Start to connect to the network..");
            new Thread(async () => await StartConnectMinerAsync()).Start();
        }

        /// <summary>
        /// Load config file.
        /// </summary>
        /// <returns></returns>
        private static bool LoadConfig()
        {
            string configContent = string.Empty;

            using (StreamReader reader = new StreamReader(GetCurrentPathConfig()))
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
                            UseProxy = true;
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
                        WalletAddress = configLine.Replace("WALLET_ADDRESS=", "");
                        if (!UseProxy)
                        {
                            WalletAddress = ClassUtils.RemoveSpecialCharacters(WalletAddress);
                            ClassConsole.WriteLine("Checking wallet address before to connect..", 4);

                            bool checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(WalletAddress).Result;
                            while (WalletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || WalletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize || !checkWalletAddress)
                            {
                                Console.WriteLine("Invalid wallet address inside your config.ini file - Please, write your wallet address to start your solo mining: ");
                                WalletAddress = Console.ReadLine();
                                WalletAddress = ClassUtils.RemoveSpecialCharacters(WalletAddress);
                                ClassConsole.WriteLine("Checking wallet address before to connect..", 4);
                                walletAddressCorrected = true;
                                checkWalletAddress = ClassTokenNetwork.CheckWalletAddressExistAsync(WalletAddress).Result;
                            }
                            if (checkWalletAddress)
                            {
                                ClassConsole.WriteLine("Wallet address: " + WalletAddress + " is valid.", 1);
                            }
                        }
                        totalConfigLine++;
                    }
                    if (configLine.Contains("MINING_THREAD=") && !miningThreadLineFound)
                    {
                        miningThreadLineFound = true;
                        if (!int.TryParse(configLine.Replace("MINING_THREAD=", ""), out TotalThreadMining))
                        {
                            ClassConsole.WriteLine("MINING_THREAD line contain an invalid integer value.", 3);
                            return false;
                        }
                        totalConfigLine++;
                    }
                    if (configLine.Contains("MINING_THREAD_PRIORITY="))
                    {
                        if (!int.TryParse(configLine.Replace("MINING_THREAD_PRIORITY=", ""), out ThreadMiningPriority))
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
                            ShareJobRange = true;
                        }
                        totalConfigLine++;
                    }
                    if (configLine.Contains("PROXY_PORT="))
                    {
                        if (!int.TryParse(configLine.Replace("PROXY_PORT=", ""), out ProxyPort))
                        {
                            ClassConsole.WriteLine("PROXY_PORT= line contain an invalid integer value.", 3);
                            return false;
                        }
                        totalConfigLine++;
                    }
                    if (configLine.Contains("PROXY_HOST="))
                    {
                        ProxyHost = configLine.Replace("PROXY_HOST=", "");
                        totalConfigLine++;
                    }
                    if (configLine.Contains("MINING_PERCENT_DIFFICULTY_START="))
                    {
                        if (!int.TryParse(configLine.Replace("MINING_PERCENT_DIFFICULTY_START=", ""), out MiningPercentDifficultyStart))
                        {
                            ClassConsole.WriteLine("MINING_PERCENT_DIFFICULTY_START= line contain an invalid integer value.", 3);
                            return false;
                        }
                        totalConfigLine++;
                    }
                    if (configLine.Contains("MINING_PERCENT_DIFFICULTY_END="))
                    {
                        if (!int.TryParse(configLine.Replace("MINING_PERCENT_DIFFICULTY_END=", ""), out MiningPercentDifficultyEnd))
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
                    return true;
                }
            }
            return false;
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
            Console.WriteLine("Write config.ini file..");
            File.Create(GetCurrentPathConfig()).Close();
            StreamWriter writeConfig = new StreamWriter(GetCurrentPathConfig())
            {
                AutoFlush = true
            };
            writeConfig.WriteLine("WALLET_ADDRESS=" + WalletAddress);
            writeConfig.WriteLine("MINING_THREAD=" + TotalThreadMining);
            writeConfig.WriteLine("MINING_THREAD_PRIORITY=" + ThreadMiningPriority);

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
            if (UseProxy)
            {
                writeConfig.WriteLine("PROXY_ENABLE=Y");
                writeConfig.WriteLine("PROXY_PORT=" + ProxyPort);
                writeConfig.WriteLine("PROXY_HOST=" + ProxyHost);
                writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_START=" + MiningPercentDifficultyStart);
                writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_END=" + MiningPercentDifficultyEnd);
            }
            else
            {
                writeConfig.WriteLine("PROXY_ENABLE=N");
                writeConfig.WriteLine("PROXY_PORT=0");
                writeConfig.WriteLine("PROXY_HOST=NONE");
                writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_START=0");
                writeConfig.WriteLine("MINING_PERCENT_DIFFICULTY_END=0");

            }
            writeConfig.Close();
        }

        /// <summary>
        /// Start to connect the miner to the network
        /// </summary>
        private static async Task<bool> StartConnectMinerAsync()
        {
            CertificateConnection = Xiropht_Connector_All.Utils.ClassUtils.GenerateCertificate();
            MalformedPacket = string.Empty;
            if (ObjectSeedNodeNetwork == null)
            {
                ObjectSeedNodeNetwork = new ClassSeedNodeConnector();
            }
            else
            {
                ObjectSeedNodeNetwork.DisconnectToSeed();
            }
            if (!UseProxy)
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
                while (!await ObjectSeedNodeNetwork.StartConnectToSeedAsync(ProxyHost, ProxyPort, IsLinux))
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
            if (!UseProxy)
            {
                ClassConsole.WriteLine("Miner connected to the network, generate certificate connection..", 2);
            }
            if (!UseProxy)
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
            if (!UseProxy)
            {
                if (PacketSpeedSend > 0)
                {
                    Thread.Sleep(PacketSpeedSend);
                }
                ClassConsole.WriteLine("Send wallet address for login your solo miner..", 2);
                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassConnectorSettingEnumeration.MinerLoginType + "|" + WalletAddress, CertificateConnection, false, true))
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
                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassConnectorSettingEnumeration.MinerLoginType + "|" + WalletAddress + "|" + MiningPercentDifficultyEnd + "|" + MiningPercentDifficultyStart + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
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
                        if (!UseProxy)
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
                        if (UseProxy)
                        {
                            if (splitPacket[1] == "YES") // Proxy want share for check them.
                            {
                                ProxyWantShare = true;
                            }
                        }
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
                                        if (!UseProxy)
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
                                        if (!UseProxy)
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
                            if (!UseProxy)
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
                        if (UseProxy)
                        {
                            if (splitBlockContent[11] == "PROXY=YES")
                            {
                                proxy = true;
                            }
                        }
                        if (CurrentBlockId != splitBlockContent[0].Replace("ID=", "") || CurrentBlockHash != splitBlockContent[1].Replace("HASH=", "") || proxy)
                        {

                            if (!UseProxy)
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
                                CanMining = true;
                                var splitCurrentBlockJob = CurrentBlockJob.Split(new[] { ";" }, StringSplitOptions.None);
                                var minRange = decimal.Parse(splitCurrentBlockJob[0]);
                                var maxRange = decimal.Parse(splitCurrentBlockJob[1]);

                                if (UseProxy)
                                {
                                    ClassConsole.WriteLine("Job range received from proxy: " + minRange + "|" + maxRange + "", 2);
                                }
                                var minProxyStart = maxRange - minRange;
                                var incrementProxyRange = minProxyStart / TotalThreadMining;
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


                                ThreadMining = null;
                                ThreadMining = new Thread[TotalThreadMining];
                                for (int i = 0; i < TotalThreadMining; i++)
                                {


                                    if (i < TotalThreadMining)
                                    {
                                        int i1 = i + 1;
                                        int iThread = i;
                                        ThreadMining[i] = new Thread(delegate ()
                                        {
                                            if (ShareJobRange)
                                            {

                                                if (UseProxy)
                                                {
                                                    if (minRange > 0)
                                                    {
                                                        decimal minRangeTmp = previousProxyMaxRange;
                                                        decimal maxRangeTmp = minRangeTmp + incrementProxyRange;
                                                        previousProxyMaxRange = maxRangeTmp;
                                                        StartMiningAsync(iThread, (decimal)Math.Round(minRangeTmp, 0), (decimal)(Math.Round(maxRangeTmp, 0)));
                                                    }
                                                    else
                                                    {

                                                        decimal minRangeTmp = (decimal)Math.Round((maxRange / TotalThreadMining) * (i1 - 1), 0);
                                                        decimal maxRangeTmp = (decimal)(Math.Round(((maxRange / TotalThreadMining) * i1), 0));
                                                        StartMiningAsync(iThread, minRangeTmp, maxRangeTmp);

                                                    }

                                                }
                                                else
                                                {
                                                    decimal minRangeTmp = (decimal)Math.Round((maxRange / TotalThreadMining) * (i1 - 1), 0);
                                                    decimal maxRangeTmp = (decimal)(Math.Round(((maxRange / TotalThreadMining) * i1), 0));
                                                    StartMiningAsync(iThread, minRangeTmp, maxRangeTmp);
                                                }

                                            }
                                            else
                                            {
                                                StartMiningAsync(iThread, minRange, maxRange);
                                            }
                                        });
                                        switch (ThreadMiningPriority)
                                        {
                                            case 0:
                                                ThreadMining[i].Priority = ThreadPriority.Lowest;
                                                break;
                                            case 1:
                                                ThreadMining[i].Priority = ThreadPriority.BelowNormal;
                                                break;
                                            case 2:
                                                ThreadMining[i].Priority = ThreadPriority.Normal;
                                                break;
                                            case 3:
                                                ThreadMining[i].Priority = ThreadPriority.AboveNormal;
                                                break;
                                            case 4:
                                                ThreadMining[i].Priority = ThreadPriority.Highest;
                                                break;
                                        }
                                        ThreadMining[i].IsBackground = true;
                                        ThreadMining[i].Start();
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
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendEnableCheckShare:
                        ClassConsole.WriteLine("Proxy want to check each share.", 2);
                        ProxyWantShare = true;
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendDisableCheckShare:
                        ClassConsole.WriteLine("Proxy don't want to check each share.", 2);
                        ProxyWantShare = false;
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendJobStatus:
                        switch (splitPacket[1])
                        {
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareUnlock:
                                TotalBlockAccepted++;
                                ClassConsole.WriteLine("Block accepted, stop mining, wait new block.", 1);
                                StopMining();
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareWrong:
                                TotalBlockRefused++;
                                ClassConsole.WriteLine("Block not accepted, stop mining, wait new block.", 3);
                                StopMining();
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareAleady:
                                if (CurrentBlockId == splitPacket[1])
                                {
                                    ClassConsole.WriteLine(splitPacket[1] + " Orphaned, someone already got it, stop mining, wait new block.", 3);
                                    StopMining();
                                }
                                else
                                {
                                    ClassConsole.WriteLine(splitPacket[1] + " Orphaned, someone already get it.", 3);
                                }
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareNotExist:
                                ClassConsole.WriteLine("Block mined does not exist, stop mining, wait new block.", 4);
                                StopMining();
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareGood:
                                TotalShareAccepted++;
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareBad:
                                ClassConsole.WriteLine("Block not accepted, someone already got it or your share is invalid.", 3);
                                TotalShareInvalid++;
                                break;
                        }

                        if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, string.Empty, false, false))
                        {
                            DisconnectNetwork();
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
                if (!UseProxy)
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
                                    GC.SuppressFinalize(ThreadMining[i]);
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
        /// Start mining.
        /// </summary>
        /// <param name="idThread"></param>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        private static async void StartMiningAsync(int idThread, decimal minRange, decimal maxRange)
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

            Console.WriteLine("Thread: " + idThread + " min range:" + minRange + " max range:" + maxRange, 2);

            Console.WriteLine("Current Mining Method: " + CurrentBlockMethod + " = AES ROUND: " + CurrentRoundAesRound + " AES SIZE: " + CurrentRoundAesSize + " AES BYTE KEY: " + CurrentRoundAesKey + " XOR KEY: " + CurrentRoundXorKey);

            while (CanMining)
            {
                try
                {
                    if (!cts.IsCancellationRequested)
                    {
                        if (CurrentBlockId != currentBlockId || currentBlockTimestamp != CurrentBlockTimestampCreate)
                        {
                            currentBlockId = CurrentBlockId;
                            currentBlockTimestamp = CurrentBlockTimestampCreate;
                            currentBlockDifficulty = decimal.Parse(CurrentBlockDifficulty);
                            currentBlockDifficultyLength = ("" + currentBlockDifficulty).Length;
                        }



                        string firstNumber = string.Empty;
                        string secondNumber = string.Empty;

                        if (ClassUtils.GetRandomBetween(0, 100) >= ClassUtils.GetRandomBetween(0, 100))
                        {
                            firstNumber = ClassUtils.GenerateNumberMathCalculation(minRange, maxRange, currentBlockDifficultyLength);
                        }
                        else
                        {
                            firstNumber = ClassUtils.GetRandomBetweenJob(minRange, maxRange).ToString("F0");
                        }


                        if (ClassUtils.GetRandomBetween(0, 100) >= ClassUtils.GetRandomBetween(0, 100))
                        {
                            secondNumber = ClassUtils.GenerateNumberMathCalculation(minRange, maxRange, currentBlockDifficultyLength);
                        }
                        else
                        {
                            secondNumber = ClassUtils.GetRandomBetweenJob(minRange, maxRange).ToString("F0");

                        }

                        decimal computeNumberSize = firstNumber.Length + secondNumber.Length;

                        for (int k = 0; k < ClassUtils.randomOperatorCalculation.Length; k++)
                        {
                            if (k < ClassUtils.randomOperatorCalculation.Length)
                            {
                                string calcul = firstNumber + " " + ClassUtils.randomOperatorCalculation[k] + " " + secondNumber;
                                decimal calculCompute = ClassUtils.ComputeCalculation(firstNumber, ClassUtils.randomOperatorCalculation[k], secondNumber);

                                string calculComputeString = calculCompute.ToString();
                                if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it. 
                                {
                                    if (calculCompute > 1 && calculCompute <= currentBlockDifficulty)
                                    {
                                        string encryptedShare = calcul;

                                        encryptedShare = MakeEncryptedShare(encryptedShare, idThread);

                                        if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                        {
                                            string hashShare = ClassUtils.GenerateSHA512(encryptedShare);

                                            TotalMiningHashrateRound[idThread]++;
                                            if (!CanMining)
                                            {
                                                return;
                                            }

                                            if (hashShare == CurrentBlockIndication)
                                            {
                                                ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                                if (!UseProxy)
                                                {
                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                        ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                        calculCompute.ToString("F0") +
                                                        "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                    {
                                                        DisconnectNetwork();
                                                        break;
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
                                                        break;
                                                    }
                                                }
                                                await Task.Factory.StartNew(StopMining).ConfigureAwait(false);
                                                break;
                                            }
                                        }
                                    }
                                    else // Test the calculation reverted.





                                    {
                                        calcul = secondNumber + " " + ClassUtils.randomOperatorCalculation[k] + " " + firstNumber;




                                        calculCompute = ClassUtils.ComputeCalculation(secondNumber, ClassUtils.randomOperatorCalculation[k], firstNumber);
                                        calculComputeString = calculCompute.ToString();
                                        if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it. 
                                        {
                                            if (calculCompute > 1 && calculCompute <= currentBlockDifficulty)
                                            {


                                                string encryptedShare = calcul;





                                                encryptedShare = MakeEncryptedShare(encryptedShare, idThread);
                                                if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                                {
                                                    string hashShare = ClassUtils.GenerateSHA512(encryptedShare);

                                                    TotalMiningHashrateRound[idThread]++;
                                                    if (!CanMining)
                                                    {
                                                        return;







                                                    }

                                                    if (hashShare == CurrentBlockIndication)
                                                    {
                                                        ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                                        if (!UseProxy)


                                                        {
                                                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                                ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                                calculCompute.ToString("F0") +
                                                                "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                            {
                                                                DisconnectNetwork();
                                                                break;
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
                                                                break;
                                                            }
                                                        }
                                                        await Task.Factory.StartNew(StopMining).ConfigureAwait(false);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                                else // Test the calculation reverted.
                                {
                                    calcul = secondNumber + " " + ClassUtils.randomOperatorCalculation[k] + " " + firstNumber;
                                    calculCompute = ClassUtils.ComputeCalculation(secondNumber, ClassUtils.randomOperatorCalculation[k], firstNumber);
                                    calculComputeString = calculCompute.ToString();
                                    if (calculCompute - Math.Round(calculCompute, 0) == 0) // Check if the result contains decimal places, if yes ignore it.
                                    {

                                        if (calculCompute > 1 && calculCompute <= currentBlockDifficulty)
                                        {


                                            string encryptedShare = calcul;


                                            encryptedShare = MakeEncryptedShare(encryptedShare, idThread);

                                            if (encryptedShare != ClassAlgoErrorEnumeration.AlgoError)
                                            {
                                                string hashShare = ClassUtils.GenerateSHA512(encryptedShare);

                                                TotalMiningHashrateRound[idThread]++;
                                                if (!CanMining)
                                                {
                                                    return;
                                                }

                                                if (hashShare == CurrentBlockIndication)
                                                {
                                                    ClassConsole.WriteLine("Exact share for unlock the block seems to be found, submit it: " + calcul + " and waiting confirmation..\n", 1);
                                                    if (!UseProxy)
                                                    {
                                                        if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                            ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                            calculCompute.ToString("F0") +
                                                            "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                        {
                                                            DisconnectNetwork();
                                                            break;
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
                                                            break;
                                                        }
                                                    }
                                                    await Task.Factory.StartNew(StopMining).ConfigureAwait(false);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {

                }
            }
        }


        /// <summary>
        /// Encrypt math calculation with the current mining method
        /// </summary>
        /// <param name="calculation"></param>
        /// <returns></returns>
        private static string MakeEncryptedShare(string calculation, int idThread)
        {
            try
            {

                string encryptedShare = ClassUtils.StringToHex(calculation + CurrentBlockTimestampCreate);

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

                // Generate SHA512 HASH for the share.
                encryptedShare = ClassUtils.GenerateSHA512(encryptedShare);

                return encryptedShare;
            }
            catch
            {
                return ClassAlgoErrorEnumeration.AlgoError;
            }
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
                                if (UseProxy) // Share hashrate information to the proxy solo miner.
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
        public static string GetCurrentPathConfig()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\config.ini";
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
