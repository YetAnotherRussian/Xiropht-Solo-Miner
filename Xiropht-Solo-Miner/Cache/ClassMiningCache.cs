using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xiropht_Solo_Miner.Utility;

namespace Xiropht_Solo_Miner.Cache
{
    public class ClassMiningCache
    {
        public List<Dictionary<string, int>> MiningListCache;
        private const int MaxMathCombinaisonPerCache = int.MaxValue-1;
        private const int RamCounterInterval = 30; // Each 30 seconds.
        private PerformanceCounter _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
        private long _lastDateRamCounted;
        private long _ramLimitInMb = 128;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassMiningCache()
        {
            MiningListCache = new List<Dictionary<string, int>>();
        }

        /// <summary>
        /// Return the number of combinaisons stored.
        /// </summary>
        public long Count => CountCombinaison();

        /// <summary>
        /// Clear mining cache.
        /// </summary>
        public void CleanMiningCache()
        {
            MiningListCache.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            MiningListCache.Add(new Dictionary<string, int>());
        }

        /// <summary>
        /// Check if mining cache combinaison exist.
        /// </summary>
        /// <param name="combinaison"></param>
        /// <returns></returns>
        public bool CheckMathCombinaison(string combinaison)
        {
            if (MiningListCache.Count > 0)
            {
                for (int i = 0; i < MiningListCache.Count; i++)
                {
                    if (i < MiningListCache.Count)
                    {
                        if (MiningListCache[i].ContainsKey(combinaison))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Insert new math combinaison
        /// </summary>
        /// <param name="combinaison"></param>
        /// <param name="idThread"></param>
        public bool InsertMathCombinaison(string combinaison, int idThread)
        {
            if (_lastDateRamCounted < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    var availbleRam = long.Parse(ClassUtility.RunCommandLineMemoryAvailable());
                    _lastDateRamCounted = DateTimeOffset.Now.ToUnixTimeSeconds() + RamCounterInterval;
                    if (availbleRam <= _ramLimitInMb)
                    {
                        return true;
                    }
                }
                else
                {

                    float availbleRam = _ramCounter.NextValue();
                    _lastDateRamCounted = DateTimeOffset.Now.ToUnixTimeSeconds() + RamCounterInterval;
                    if (availbleRam <= _ramLimitInMb)
                    {
                        return true;
                    }
                }
            }

            try
            {
                if (MiningListCache.Count > 0)
                {
                    bool inserted = false;
                    for (int i = 0; i < MiningListCache.Count; i++)
                    {
                        if (!inserted)
                        {
                            if (i < MiningListCache.Count)
                            {
                                if (MiningListCache[i].Count < MaxMathCombinaisonPerCache)
                                {
                                    if (MiningListCache[i].ContainsKey(combinaison))
                                    {
                                        return false;
                                    }

                                    MiningListCache[i].Add(combinaison, idThread);
                                    inserted = true;
                                }
                            }
                        }
                    }

                    if (!inserted)
                    {
                        MiningListCache.Add(new Dictionary<string, int>());
                        if (!MiningListCache[MiningListCache.Count -1].ContainsKey(combinaison))
                        {
                            MiningListCache[MiningListCache.Count - 1].Add(combinaison, idThread);
                            return true;
                        }
                    }
                }
                else
                {
                    MiningListCache.Add(new Dictionary<string, int>());
                    if (!MiningListCache[0].ContainsKey(combinaison))
                    {
                        MiningListCache[0].Add(combinaison, idThread);
                        return true;
                    }

                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get total combinaison
        /// </summary>
        /// <returns></returns>
        public long CountCombinaison()
        {
            long totalCombinaison = 0;
            if (MiningListCache.Count > 0)
            {
                for (int i = 0; i < MiningListCache.Count; i++)
                {
                    if (i < MiningListCache.Count)
                    {
                        totalCombinaison += MiningListCache[i].Count;
                    }
                }
            }

            return totalCombinaison;
        }

    }
}
