namespace Xiropht_Solo_Miner
{
    public class ClassMinerConfig
    {
        public string mining_wallet_address = string.Empty;
        public int mining_thread;
        public int mining_thread_priority = 2;
        public bool mining_thread_spread_job;
        public bool mining_enable_proxy;
        public int mining_proxy_port;
        public string mining_proxy_host = string.Empty;
        public int mining_percent_difficulty_start;
        public int mining_percent_difficulty_end;
    }
}
