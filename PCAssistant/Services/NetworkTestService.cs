using System.Net.NetworkInformation;
using System.Net;

namespace PCAssistant.Services
{
    public class NetworkTestService
    {
        private readonly Logger _logger;
        private const int PING_COUNT = 60; // 1分钟，每秒一次
        private const int PING_TIMEOUT = 1000; // 1秒超时
        private const string TEST_DOMAIN = "baidu.com";

        public class NetworkTestResult
        {
            public string LocalIP { get; set; } = "";
            public string Gateway { get; set; } = "";
            public bool HasInternetAccess { get; set; }
            public double GatewayAveragePing { get; set; }
            public double InternetAveragePing { get; set; }
            public int GatewayLostPackets { get; set; }
            public int InternetLostPackets { get; set; }
        }

        public NetworkTestService()
        {
            _logger = Logger.Instance;
        }

        public async Task<NetworkTestResult> RunNetworkTestAsync(IProgress<string> progress)
        {
            var result = new NetworkTestResult();
            
            try
            {
                // 1. 获取本地IP和网关
                _logger.Info("开始获取网络信息...");
                progress.Report("正在获取网络信息...");
                
                var networkInfo = GetNetworkInfo();
                result.LocalIP = networkInfo.Item1;
                result.Gateway = networkInfo.Item2;
                
                _logger.Info($"本地IP: {result.LocalIP}");
                _logger.Info($"网关地址: {result.Gateway}");

                // 2. 测试外网连通性
                _logger.Info("测试外网连通性...");
                progress.Report("正在测试外网连通性...");
                
                result.HasInternetAccess = await CheckInternetAccessAsync();
                _logger.Info($"外网连通性: {(result.HasInternetAccess ? "正常" : "异常")}");

                if (!result.HasInternetAccess)
                {
                    _logger.Warning("外网连接失败，跳过后续测试");
                    return result;
                }

                // 3. Ping网关
                _logger.Info("开始测试网关延迟...");
                progress.Report("正在测试网关延迟...");
                
                var gatewayResult = await PingHostAsync(result.Gateway, "网关");
                result.GatewayAveragePing = gatewayResult.Item1;
                result.GatewayLostPackets = gatewayResult.Item2;

                // 4. Ping外网
                _logger.Info("开始测试外网延迟...");
                progress.Report("正在测试外网延迟...");
                
                var internetResult = await PingHostAsync(TEST_DOMAIN, "外网");
                result.InternetAveragePing = internetResult.Item1;
                result.InternetLostPackets = internetResult.Item2;

                _logger.Info("网络测试完成");
                progress.Report("网络测试完成");
            }
            catch (Exception ex)
            {
                _logger.Error($"网络测试过程中发生错误: {ex.Message}");
                throw;
            }

            return result;
        }

        private (string, string) GetNetworkInfo()
        {
            string localIP = "未知";
            string gateway = "未知";

            try
            {
                // 获取本地IP和网关
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                localIP = ip.Address.ToString();
                                
                                // 获取网关
                                var gateways = ni.GetIPProperties().GatewayAddresses;
                                if (gateways.Count > 0)
                                {
                                    gateway = gateways[0].Address.ToString();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"获取网络信息时出错: {ex.Message}");
            }

            return (localIP, gateway);
        }

        private async Task<bool> CheckInternetAccessAsync()
        {
            try
            {
                using var client = new WebClient();
                await client.DownloadStringTaskAsync("http://www.baidu.com");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<(double, int)> PingHostAsync(string host, string hostType)
        {
            var pingTimes = new List<long>();
            int lostPackets = 0;
            
            using var ping = new Ping();
            
            _logger.Info($"开始测试{hostType}延迟（共60次）...");
            
            for (int i = 0; i < PING_COUNT; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync(host, PING_TIMEOUT);
                    if (reply.Status == IPStatus.Success)
                    {
                        pingTimes.Add(reply.RoundtripTime);
                    }
                    else
                    {
                        lostPackets++;
                    }
                }
                catch (Exception)
                {
                    lostPackets++;
                }

                await Task.Delay(1000); // 等待1秒
            }

            double averagePing = pingTimes.Any() ? pingTimes.Average() : -1;
            
            _logger.Info($"{hostType}延迟测试完成：");
            _logger.Info($"平均延迟: {(averagePing >= 0 ? $"{averagePing:F2}ms" : "测试失败")}");
            _logger.Info($"丢包数: {lostPackets}");
            
            return (averagePing, lostPackets);
        }
    }
} 