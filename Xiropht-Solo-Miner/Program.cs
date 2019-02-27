using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Seed;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.SoloMining;

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
        private static int MiningDifficulty;
        private static int MiningPositionDifficulty;

        /// <summary>
        /// Threading.
        /// </summary>
        private const int ThreadCheckNetworkInterval = 1 * 1000; // Check each 5 seconds.
        private static int TotalThreadMining;
        private static Thread ThreadListenNetwork;
        private static Thread ThreadCheckNetwork;
        private static Thread[] ThreadMining;
        private static CancellationTokenSource cts = new CancellationTokenSource();
        public static List<int> TotalMiningRound = new List<int>();
        public static List<int> TotalMiningHashrateRound = new List<int>();
        public static int TotalHashrate;
        public static int TotalCalculation;
        public static int ThreadMiningPriority;

        /// <summary>
        /// Wallet mining.
        /// </summary>
        private static string WalletAddress;

        private static bool IsLinux;

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Thread.CurrentThread.Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
            ClassConsole.WriteLine("Xiropht Solo Miner - " + Assembly.GetExecutingAssembly().GetName().Version + "b", 4);

            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                IsLinux = true;
            }
#if DEBUG
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args2)
            {
                var filePath =  ConvertPath(Directory.GetCurrentDirectory() + "\\error_miner.txt");
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
#endif
            ThreadMiningPriority = 2; // By Default
            MiningDifficulty = 0; // By Default
            MiningPositionDifficulty = 0; // By Default
            if (File.Exists(GetCurrentPathConfig()))
            {
                StreamReader reader = new StreamReader(GetCurrentPathConfig());
                string line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("WALLET_ADDRESS="))
                    {
                        WalletAddress = line.Replace("WALLET_ADDRESS=", "");
                    }
                    else if (line.Contains("MINING_THREAD="))
                    {
                        TotalThreadMining = int.Parse(line.Replace("MINING_THREAD=", ""));
                    }
                    else if (line.Contains("MINING_THREAD_PRIORITY="))
                    {
                        ThreadMiningPriority = int.Parse(line.Replace("MINING_THREAD_PRIORITY=", ""));
                    }
                    else if (line.Contains("MINING_THREAD_SPREAD_JOB="))
                    {
                        if (line.Replace("MINING_THREAD_SPREAD_JOB=", "") == "Y" || line.Replace("MINING_THREAD_SPREAD_JOB=", "") == "y")
                        {
                            ShareJobRange = true;
                        }
                    }
                    else if (line.Contains("PROXY_ENABLE="))
                    {
                        if (line.Replace("PROXY_ENABLE=", "") == "Y" || line.Replace("PROXY_ENABLE=", "") == "y")
                        {
                            UseProxy = true;
                        }
                    }
                    else if (line.Contains("PROXY_PORT="))
                    {
                        ProxyPort = int.Parse(line.Replace("PROXY_PORT=", ""));
                    }
                    else if (line.Contains("PROXY_HOST="))
                    {
                        ProxyHost = line.Replace("PROXY_HOST=", "");
                    }
                    else if (line.Contains("MINING_DIFFICULTY="))
                    {
                        MiningDifficulty = int.Parse(line.Replace("MINING_DIFFICULTY=", ""));
                    }
                    else if (line.Contains("MINING_POSITION_DIFFICULTY="))
                    {
                        MiningPositionDifficulty = int.Parse(line.Replace("MINING_POSITION_DIFFICULTY=", ""));
                    }
                }
                ThreadMining = new Thread[TotalThreadMining];


                TotalMiningRound = new List<int>();
                TotalMiningHashrateRound = new List<int>();
                for (int i = 0; i < TotalThreadMining; i++)
                {
                    if (i < TotalThreadMining)
                    {
                        TotalMiningRound.Add(0);
                        TotalMiningHashrateRound.Add(0);
                    }
                }




                Console.WriteLine("Start to connect to the network..");
                new Thread(async () => await StartConnectMinerAsync()).Start();
            }
            else
            {
                Console.WriteLine("Please, write your wallet address for start your solo mining: ");
                WalletAddress = Console.ReadLine();
                Console.WriteLine("How many thread do you want to run? Number of core detecting: " +
                                  Environment.ProcessorCount);

                var tmp = Console.ReadLine();
                if (!int.TryParse(tmp, out TotalThreadMining))
                {
                    TotalThreadMining = Environment.ProcessorCount;
                }
                ThreadMining = new Thread[TotalThreadMining];

                Console.WriteLine("Do you want use a proxy instead seed node? [Y/N]");
                var choose = Console.ReadLine();

                if (choose == "Y" || choose == "y")
                {
                    Console.WriteLine("Write the IP/HOST of your mining proxy: ");
                    ProxyHost = Console.ReadLine();
                    Console.WriteLine("Write the port of your mining proxy: ");

                    while(!int.TryParse(Console.ReadLine(), out ProxyPort))
                    {
                        Console.WriteLine("This is not a port number, please try again: ");
                    }
                    Console.WriteLine("Do you want select a mining range difficulty? [Y/N]");
                    choose = Console.ReadLine();
                    if (choose == "Y" || choose == "y")
                    {
                        Console.WriteLine("Select a mining range difficulty between 1% to 100%, select your range: ");
                        while (!int.TryParse(Console.ReadLine(), out MiningDifficulty))
                        {
                            Console.WriteLine("This is not a number, please try again: ");
                        }
                        if (MiningDifficulty < 1)
                        {
                            MiningDifficulty = 1;
                        }
                        else if (MiningDifficulty > 100)
                        {
                            MiningDifficulty = 100;
                        }
                        Console.WriteLine("Select a mining position difficulty for your difficulty, Min: 0, Max: 99");
                        while(!int.TryParse(Console.ReadLine(), out MiningPositionDifficulty))
                        {
                            Console.WriteLine("This is not a number, please try again: ");
                        }
                        if (MiningPositionDifficulty > 99)
                        {
                            MiningPositionDifficulty = 0;
                        }
                        if (MiningPositionDifficulty < 0)
                        {
                            MiningPositionDifficulty = 0;
                        }
                    }

                    UseProxy = true;
                }
                Console.WriteLine("Do you want share job range per thread ? [Y/N]");
                choose = Console.ReadLine();
                if (choose == "Y" || choose == "y")
                {
                    ShareJobRange = true;
                }
                for (int i = 0; i < TotalThreadMining; i++)
                {
                    if (i < TotalThreadMining)
                    {
                        TotalMiningRound.Add(0);
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

            ClassConsole.WriteLine("Command Line: h -> show hashrate.", 4);
            ClassConsole.WriteLine("Command Line: d -> show current difficulty.", 4);

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
                writeConfig.WriteLine("MINING_DIFFICULTY=" + MiningDifficulty);
                writeConfig.WriteLine("MINING_POSITION_DIFFICULTY=" + MiningPositionDifficulty);
            }
            else
            {
                writeConfig.WriteLine("PROXY_ENABLE=N");
                writeConfig.WriteLine("PROXY_PORT=0");
                writeConfig.WriteLine("PROXY_HOST=NONE");
                writeConfig.WriteLine("MINING_DIFFICULTY=0");
                writeConfig.WriteLine("MINING_POSITION_DIFFICULTY=0");

            }
            writeConfig.Close();
        }

        /// <summary>
        /// Start to connect the miner to the network
        /// </summary>
        private static async Task StartConnectMinerAsync()
        {
            CertificateConnection = Xiropht_Connector_All.Utils.ClassUtils.GenerateCertificate();

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
                    Thread.Sleep(5000);
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
                    Thread.Sleep(5000);
                }
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 0)
                {
                    PacketSpeedSend = (int)stopwatch.ElapsedMilliseconds;
                }
            }
            IsConnected = true;
            if (!UseProxy)
            {
                ClassConsole.WriteLine("Miner connected to the network, generate certificate connection..", 2);
            }
            if (!UseProxy)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(CertificateConnection, string.Empty, false, false);
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
                await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync("MINER|" + WalletAddress, CertificateConnection, false, true);
            }
            else
            {
                if (PacketSpeedSend > 0)
                {
                    Thread.Sleep(PacketSpeedSend);
                }
                ClassConsole.WriteLine("Send wallet address for login your solo miner..", 2);
                await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync("MINER|" + WalletAddress + "|"+MiningDifficulty+"|"+MiningPositionDifficulty+"|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false);
            }
            ListenNetwork();
            CheckNetwork();

            if (!_checkNetworkEnabled)
            {
                _checkNetworkEnabled = true;
                CalculateHashrate();
            }
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

                Thread.Sleep(5000);
                while (true)
                {
                    if (UseProxy)
                    {
                        if (LastPacketReceived + TimeoutPacketReceived < DateTimeOffset.Now.ToUnixTimeSeconds())
                        {
                            ClassConsole.WriteLine("Network connection timeout.", 3);
                            ObjectSeedNodeNetwork?.DisconnectToSeed();
                        }
                        else
                        {
                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskListBlockMethod, string.Empty, false, false))
                            {
                                DisconnectNetwork();
                            }
                        }
                    }
                    if (!IsConnected || !LoginAccepted || !ObjectSeedNodeNetwork.ReturnStatus())
                    {
                        ClassConsole.WriteLine("Network connection lost or aborted, retry to connect..", 3);
                        await StopMiningAsync();
                        CurrentBlockId = "";
                        CurrentBlockHash = "";
                        new Thread(async () => await StartConnectMinerAsync()).Start();
                        break;
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
            ObjectSeedNodeNetwork.DisconnectToSeed();
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
                            string packet =  await ObjectSeedNodeNetwork.ReceivePacketFromSeedNodeAsync(CertificateConnection, false, true);

                            if (packet.Contains("*"))
                            {
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
                                                    await Task.Run(async () => await HandlePacketMiningAsync(packetRequest));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (packet == ClassSeedNodeStatus.SeedError)
                                {
                                    ClassConsole.WriteLine("Network error received. Waiting network checker..", 3);
                                    DisconnectNetwork();
                                    break;
                                }
                                if (packet != ClassSeedNodeStatus.SeedNone && packet != ClassSeedNodeStatus.SeedError)
                                {
                                    await Task.Run(async () => await HandlePacketMiningAsync(packet));
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
                                await Task.Run(async () => await HandlePacketMiningAsync(packet));
                            }
                        }
                    }
                    catch(Exception error)
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
                        ClassConsole.WriteLine("Miner login accepted, start to mining..", 1);
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

                                foreach (var methodName in splitMethodList)
                                {
                                    if (!string.IsNullOrEmpty(methodName))
                                    {
                                        if (ListeMiningMethodName.Contains(methodName) == false)
                                        {
                                            ListeMiningMethodName.Add(methodName);
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
                        }
                        else
                        {
                            if (ListeMiningMethodName.Contains(methodList) == false)
                            {
                                ListeMiningMethodName.Add(methodList);
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
                        }
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendContentBlockMethod:
                        ListeMiningMethodContent.Add(splitPacket[1]);
                        break;
                    case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendCurrentBlockMining:

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
                                ClassConsole.WriteLine("New block to mining " + splitBlockContent[0], 2);
                            }
                            else
                            {
                                ClassConsole.WriteLine("Current block to mining " + splitBlockContent[0], 2);
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
                                await StopMiningAsync();
                                CanMining = true;
                                var splitCurrentBlockJob = CurrentBlockJob.Split(new[] { ";" }, StringSplitOptions.None);
                                var minRange = float.Parse(splitCurrentBlockJob[0]);
                                var maxRange = float.Parse(splitCurrentBlockJob[1]);

                                if (UseProxy)
                                {
                                    ClassConsole.WriteLine("Job range received from proxy: " + minRange + "|" + maxRange + "", 2);
                                }
                                var minProxyStart = maxRange - minRange;
                                var incrementProxyRange = minProxyStart / TotalThreadMining;
                                var previousProxyMaxRange = minRange;

                                cts = new CancellationTokenSource();
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
                                                        var minRangeTmp = previousProxyMaxRange;
                                                        var maxRangeTmp = minRangeTmp + incrementProxyRange;
                                                        previousProxyMaxRange = maxRangeTmp;
                                                        StartMiningAsync(iThread, (float)Math.Round(minRangeTmp, 0), (float)(Math.Round(maxRangeTmp, 0)));
                                                    }
                                                    else
                                                    {
                                                        StartMiningAsync(iThread, (float)Math.Round((maxRange / TotalThreadMining) * (i1 - 1), 0), (float)(Math.Round(((maxRange / TotalThreadMining) * i1), 0)));

                                                    }

                                                }
                                                else
                                                {
                                                    StartMiningAsync(iThread, (float)Math.Round((maxRange / TotalThreadMining) * (i1 - 1), 0), (float)(Math.Round(((maxRange / TotalThreadMining) * i1), 0)));
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
                                await StopMiningAsync();
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
                                await StopMiningAsync();
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareWrong:
                                TotalBlockRefused++;
                                ClassConsole.WriteLine("Block not accepted, stop mining, wait new block.", 3);
                                await StopMiningAsync();
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareAleady:
                                if (CurrentBlockId == splitPacket[1])
                                {
                                    ClassConsole.WriteLine(splitPacket[1] + " Orphaned, someone already get it, stop mining, wait new block.", 3);
                                    await StopMiningAsync();
                                }
                                else
                                {
                                    ClassConsole.WriteLine(splitPacket[1] + " Orphaned, someone already get it.", 3);
                                }
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareNotExist:
                                ClassConsole.WriteLine("Block mined not exist, stop mining, wait new block.", 4);
                                await StopMiningAsync();
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareGood:
                                TotalShareAccepted++;
                                break;
                            case ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareBad:
                                ClassConsole.WriteLine("Invalid share.", 3);
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

            new Thread(async delegate ()
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
            }).Start();
        }

        /// <summary>
        /// Stop mining.
        /// </summary>
        private static async Task StopMiningAsync()
        {
            CanMining = false;
            cts.Cancel();
            await Task.Delay(1000);
        }

        /// <summary>
        /// Start mining.
        /// </summary>
        /// <param name="idThread"></param>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        private static void StartMiningAsync(int idThread, float minRange, float maxRange)
        {
            if (minRange <= 1)
            {
                minRange = 2;
            }
            var randomOperatorCalculation = new[] { "+", "*", "%", "-", "/" };

            var randomNumberCalculation = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
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
            else
            {
                idMethod = 0;
            }
            while (ListeMiningMethodName.Count == 0)
            {
                ClassConsole.WriteLine("No method content received, waiting to receive them before..", 2);
                Thread.Sleep(1000);
            }
            while (ListeMiningMethodContent.Count < idMethod)
            {
                ClassConsole.WriteLine(
                     "The method content of: " + ListeMiningMethodName[idMethod] + " is not yet received.", 2);
                Thread.Sleep(1000);
            }

            Console.WriteLine("Thread: " + idThread + " Method used for mining block: " + ListeMiningMethodName[idMethod] + " min range:" + minRange + " max range:" + maxRange, 2);
            var splitMethod = ListeMiningMethodContent[idMethod].Split(new[] { "#" }, StringSplitOptions.None);

            int roundMethod = int.Parse(splitMethod[0]);
            int roundSize = int.Parse(splitMethod[1]);
            string roundKey = splitMethod[2];
            int keyXorMethod = int.Parse(splitMethod[3]);

            var currentBlockId = CurrentBlockId;
            var currentBlockTimestamp = CurrentBlockTimestampCreate;

            var currentBlockDifficulty = float.Parse(CurrentBlockDifficulty);

            var currentBlockDifficultyLength = ("" + currentBlockDifficulty).Length;

            StringBuilder numberBuilder = new StringBuilder();

            while (CanMining)
            {

                if (CurrentBlockId != currentBlockId || currentBlockTimestamp != CurrentBlockTimestampCreate)
                {
                    currentBlockId = CurrentBlockId;
                    currentBlockTimestamp = CurrentBlockTimestampCreate;
                }


                string firstNombre = "0";
                while (float.Parse(firstNombre) > maxRange || float.Parse(firstNombre) <= 1 || firstNombre.Length >= currentBlockDifficultyLength)
                {
                    var randomJobSize = ("" + ClassUtils.GetRandomBetweenJob(minRange, maxRange)).Length;

                    int randomSize = ClassUtils.GetRandomBetween(1, randomJobSize);
                    int counter = 0;
                    while (counter < randomSize)
                    {
                        if (randomSize > 1)
                        {
                            var numberRandom = randomNumberCalculation[
                                ClassUtils.GetRandomBetween(0, randomNumberCalculation.Length - 1)];
                            if (counter == 0)
                            {
                                while (numberRandom == "0")
                                {
                                    numberRandom = randomNumberCalculation[
                                        ClassUtils.GetRandomBetween(0, randomNumberCalculation.Length - 1)];
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
                                               ClassUtils.GetRandomBetween(0, randomNumberCalculation.Length - 1)]);
                        }
                        counter++;
                    }
                    firstNombre = numberBuilder.ToString();
                    numberBuilder.Clear();
                }


                string secondNombre = "0";


                while (float.Parse(secondNombre) > maxRange || float.Parse(secondNombre) <= 1)
                {
                    var randomJobSize = ("" + ClassUtils.GetRandomBetweenJob(minRange, maxRange)).Length;
                    int randomSize = ClassUtils.GetRandomBetween(1, randomJobSize);
                    int counter = 0;
                    while (counter < randomSize)
                    {
                        if (randomSize > 1)
                        {
                            var numberRandom = randomNumberCalculation[
                                ClassUtils.GetRandomBetween(0, randomNumberCalculation.Length - 1)];
                            if (counter == 0)
                            {
                                while (numberRandom == "0")
                                {
                                    numberRandom = randomNumberCalculation[
                                        ClassUtils.GetRandomBetween(0, randomNumberCalculation.Length - 1)];
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
                                    ClassUtils.GetRandomBetween(0, randomNumberCalculation.Length - 1)]);
                        }
                        counter++;
                    }
                    secondNombre = numberBuilder.ToString();
                    numberBuilder.Clear();
                }


                float computeNumberSize = firstNombre.Length + secondNombre.Length;

                if (computeNumberSize >= currentBlockDifficultyLength - 1 && computeNumberSize <= currentBlockDifficultyLength)
                {
                    for (int k = 0; k < randomOperatorCalculation.Length; k++)
                    {
                        if (k < randomOperatorCalculation.Length)
                        {
                            string calcul = firstNombre + " " + randomOperatorCalculation[k] + " " + secondNombre;

                            float calculCompute = 0;
                            if (calcul.Contains("+"))
                            {
                                calculCompute = float.Parse(firstNombre) + float.Parse(secondNombre);
                                calculCompute = (float)Math.Round(calculCompute, 0);
                            }
                            else if (calcul.Contains("*"))
                            {
                                calculCompute = float.Parse(firstNombre) * float.Parse(secondNombre);
                                calculCompute = (float)Math.Round(calculCompute, 0);
                            }
                            else if (calcul.Contains("%"))
                            {
                                calculCompute = float.Parse(firstNombre) % float.Parse(secondNombre);
                                calculCompute = (float)Math.Round(calculCompute, 0);
                            }
                            else if (calcul.Contains("-"))
                            {
                                calculCompute = float.Parse(firstNombre) - float.Parse(secondNombre);
                                calculCompute = (float)Math.Round(calculCompute, 0);
                            }
                            else if (calcul.Contains("/"))
                            {
                                calculCompute = float.Parse(firstNombre) / float.Parse(secondNombre);
                                calculCompute = (float)Math.Round(calculCompute, 0);
                            }

                            if (calculCompute > 1 && calculCompute <= currentBlockDifficulty)
                            {


                                string encryptedShare = calcul;


                                encryptedShare = ClassUtils.StringToHex(encryptedShare + CurrentBlockTimestampCreate);
                                encryptedShare = ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Xor, encryptedShare, "" + keyXorMethod, roundSize, null);
                                for (int i = 0; i < roundMethod; i++)
                                {
                                    encryptedShare = ClassAlgo.GetEncryptedResult(CurrentBlockAlgorithm, encryptedShare, CurrentBlockKey, roundSize, Encoding.UTF8.GetBytes(roundKey));
                                }
                                encryptedShare = ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Xor, encryptedShare, "" + keyXorMethod, roundSize, null);
                                encryptedShare = ClassAlgo.GetEncryptedResult(CurrentBlockAlgorithm, encryptedShare, CurrentBlockKey, roundSize, Encoding.UTF8.GetBytes(roundKey));

                                encryptedShare = ClassUtils.GenerateSHA512(encryptedShare);
                                string hashShare = ClassUtils.GenerateSHA512(encryptedShare);

                                TotalMiningHashrateRound[idThread]++;
                                if (!CanMining)
                                {
                                    return;
                                }

                                if (!ProxyWantShare)
                                {
                                    if (hashShare == CurrentBlockIndication)
                                    {

                                        ClassConsole.WriteLine("Share for unlock the block seems to be found, submit it: " + calcul + " \n", 1);


                                        if (!UseProxy)
                                        {
                                            new Thread(async delegate ()
                                            {

                                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                    ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                    calculCompute +
                                                    "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                {
                                                    DisconnectNetwork();
                                                }
                                            }).Start();
                                        }
                                        else
                                        {
                                            new Thread(async delegate ()
                                            {
                                                if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                 ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                 calculCompute +
                                                 "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                                {
                                                    DisconnectNetwork();
                                                }
                                            }).Start();
                                        }
                                    }
                                }
                                else
                                {


                                    new Thread(async delegate ()
                                    {
                                        if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                        ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                        calculCompute +
                                        "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                        {
                                            DisconnectNetwork();
                                        }
                                    }).Start();
                                }

                            }
                            else
                            {
                                calcul = secondNombre + " " + randomOperatorCalculation[k] + " " + firstNombre;

                                calculCompute = 0;
                                if (calcul.Contains("+"))
                                {
                                    calculCompute = float.Parse(secondNombre) + float.Parse(firstNombre);
                                    calculCompute = (float)Math.Round(calculCompute, 0);
                                }
                                else if (calcul.Contains("*"))
                                {
                                    calculCompute = float.Parse(secondNombre) * float.Parse(firstNombre);
                                    calculCompute = (float)Math.Round(calculCompute, 0);
                                }
                                else if (calcul.Contains("%"))
                                {
                                    calculCompute = float.Parse(secondNombre) % float.Parse(firstNombre);
                                    calculCompute = (float)Math.Round(calculCompute, 0);
                                }
                                else if (calcul.Contains("-"))
                                {
                                    calculCompute = float.Parse(secondNombre) - float.Parse(firstNombre);
                                    calculCompute = (float)Math.Round(calculCompute, 0);
                                }
                                else if (calcul.Contains("/"))
                                {
                                    calculCompute = float.Parse(secondNombre) / float.Parse(firstNombre);
                                    calculCompute = (float)Math.Round(calculCompute, 0);
                                }


                                if (calculCompute > 1 && calculCompute <= currentBlockDifficulty)
                                {


                                    string encryptedShare = calcul;


                                    encryptedShare = ClassUtils.StringToHex(encryptedShare + CurrentBlockTimestampCreate);
                                    encryptedShare = ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Xor, encryptedShare, "" + keyXorMethod, roundSize, null);
                                    for (int i = 0; i < roundMethod; i++)
                                    {
                                        encryptedShare = ClassAlgo.GetEncryptedResult(CurrentBlockAlgorithm, encryptedShare, CurrentBlockKey, roundSize, Encoding.UTF8.GetBytes(roundKey));
                                    }

                                    encryptedShare = ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Xor, encryptedShare, "" + keyXorMethod, roundSize, null);
                                    encryptedShare = ClassAlgo.GetEncryptedResult(CurrentBlockAlgorithm, encryptedShare, CurrentBlockKey, roundSize, Encoding.UTF8.GetBytes(roundKey));

                                    encryptedShare = ClassUtils.GenerateSHA512(encryptedShare);
                                    string hashShare = ClassUtils.GenerateSHA512(encryptedShare);

                                    TotalMiningHashrateRound[idThread]++;
                                    if (!CanMining)
                                    {
                                        return;
                                    }

                                    if (!ProxyWantShare)
                                    {
                                        if (hashShare == CurrentBlockIndication)
                                        {

                                            ClassConsole.WriteLine("Share for unlock the block seems to be found, submit it: " + calcul + " \n", 1);


                                            if (!UseProxy)
                                            {
                                                new Thread(async delegate ()
                                                {

                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                        ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                        calculCompute +
                                                        "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, CertificateConnection, false, true))
                                                    {
                                                        DisconnectNetwork();
                                                    }
                                                }).Start();
                                            }
                                            else
                                            {
                                                new Thread(async delegate ()
                                                {
                                                    if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                                     ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                                     calculCompute +
                                                     "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                                    {
                                                        DisconnectNetwork();
                                                    }
                                                }).Start();
                                            }
                                        }
                                    }
                                    else
                                    {


                                        new Thread(async delegate ()
                                        {
                                            if (!await ObjectSeedNodeNetwork.SendPacketToSeedNodeAsync(
                                            ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveJob + "|" + encryptedShare + "|" +
                                            calculCompute +
                                            "|" + calcul + "|" + hashShare + "|" + CurrentBlockId + "|" + Assembly.GetExecutingAssembly().GetName().Version, string.Empty, false, false))
                                            {
                                                DisconnectNetwork();
                                            }
                                        }).Start();
                                    }
                                }
                            }
                        }
                        TotalMiningRound[idThread]++;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the hashrate of the solo miner.
        /// </summary>
        private static void CalculateHashrate()
        {

            new Thread(() =>
             {
                 var counterTime = 0;
                 while (true)
                 {
                     try
                     {

                         int totalRound = 1;
                         int totalRoundHashrate = 1;
                         for (int i = 0; i < TotalMiningRound.Count; i++)
                         {
                             if (i < TotalMiningRound.Count)
                             {
                                 totalRound += TotalMiningRound[i];
                                 if (counterTime >= HashrateIntervalCalculation && CanMining)
                                 {
                                     ClassConsole.WriteLine("Calculation Speed Thread " + i + " : " + (TotalMiningRound[i]) + " C/s", 4);
                                 }
                             }
                         }

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


                         TotalCalculation = (totalRound);
                         TotalHashrate = (totalRoundHashrate);
                         float accuratePourcent = 0;
                         if (TotalHashrate != 0 && TotalCalculation != 0)
                         {
                             accuratePourcent = ((float)TotalHashrate / (float)TotalCalculation) * 100;
                             accuratePourcent = (float)Math.Round(accuratePourcent, 2);
                         }
                         for (int i = 0; i < TotalMiningRound.Count; i++)
                         {
                             if (i < TotalMiningRound.Count)
                             {
                                 TotalMiningRound[i] = 0;
                             }
                         }
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
                                 if (!UseProxy)
                                 {
                                     ClassConsole.WriteLine("Mining Speed: " + TotalCalculation + " C/s | " + TotalHashrate + " H/s | Accurate Rate " + accuratePourcent + "% > UNLOCK[" + TotalBlockAccepted + "] REFUSED[" + TotalBlockRefused + "]", 4);
                                 }
                                 else
                                 {
                                     if (ProxyWantShare)
                                     {
                                         ClassConsole.WriteLine("Mining Speed: " + TotalCalculation + " C/s | " + TotalHashrate + " H/s | Accurate Rate " + accuratePourcent + "% > GOOD[" + TotalShareAccepted + "] INVALID[" + TotalShareInvalid + "]", 4);
                                     }
                                     else
                                     {
                                         ClassConsole.WriteLine("Mining Speed: " + TotalCalculation + " C/s | " + TotalHashrate + " H/s | Accurate Rate " + accuratePourcent + "% > UNLOCK[" + TotalBlockAccepted + "] REFUSED[" + TotalBlockRefused + "]", 4);
                                     }
                                 }
                             }
                             if (counterTime < HashrateIntervalCalculation)
                             {
                                 counterTime++;
                             }
                             else
                             {
                                 counterTime = 0;
                             }
                         }

                     }
                     catch
                     {
                     }
                     Thread.Sleep(1000); // Each 10 seconds
                }
             }).Start();
        }

        /// <summary>
        /// Get current path of the miner.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentPathConfig()
        {
            string path = Directory.GetCurrentDirectory() + "\\config.ini";
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
