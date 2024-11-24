using System.Security.Principal;
using System.Diagnostics;
using System.IO;
using PCAssistant.Services;

namespace PCAssistant
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            Logger.Initialize(LogTxtbox); // 初始化日志系统
            
            // 添加定时器更新状态栏时间
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 每秒更新一次
            timer.Tick += (s, e) => {
                toolStripStatusLabel1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };
            timer.Start();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                Logger.Instance.Warning("程序未以管理员权限运行，部分功能可能受限");
            }

            Logger.Instance.Info("应用程序启动");

            try
            {
                ComputerInfo ci = new  ComputerInfo();
                
                // 显示信息到界面
                txtComputerInfo.Text = ci.GetInfoString();
                
                // 保存到JSON文件
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string jsonPath = Path.Combine(baseDir, "ComputerInfo");
                Directory.CreateDirectory(jsonPath);
                
                string fileName = $"ComputerInfo_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = Path.Combine(jsonPath, fileName);
                
          
                
                Logger.Instance.Info("已加载并保存系统信息");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"处理系统信息时发生错误: {ex.Message}");
            }
        }

        private async void 服务器设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                服务器设置ToolStripMenuItem.Enabled = false;
                Logger.Instance.Info("正在读取微信信息...");

                var wxinfo = await WxInfo.ReadInfoAsync(isLogging: true, isSave: true);

                Logger.Instance.Info("微信信息读取完成");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"读取微信信息时发生错误: {ex.Message}");
            }
            finally
            {
                服务器设置ToolStripMenuItem.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Logger.Instance.Info("应用程序正在关闭");
            Logger.Instance.Dispose();
        }

        private async void ActivationSystemBtn_Click(object sender, EventArgs e)
        {
            Logger.Instance.Info("开始检查系统激活状态...");

            if (!IsAdministrator())
            {
                Logger.Instance.Info("尝试以管理员权限重新启动程序...");
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        Verb = "runas", // 以管理员权限启动
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);
                    Application.Exit(); // 关闭当前实例
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"提升权限失败: {ex.Message}");
                    MessageBox.Show("需要管理员权限才能执行此操作！\n请右键以管理员身份运行本程序。",
                                  "权限不足",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                ActivationSystemBtn.Enabled = false;
                Logger.Instance.Info("开始执行系统激活...");

                // 执行激活命令
                await Task.Run(() =>
                {
                    string[] commands = {
                        "cscript //nologo slmgr.vbs /ipk W269N-WFGWX-YVC9B-4J6C9-T83GX",
                        "cscript //nologo slmgr.vbs /skms kms.03k.org",
                        "cscript //nologo slmgr.vbs /ato"
                    };

                    foreach (string cmd in commands)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {cmd}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = @"c:\windows\system32\"
                        };

                        using (Process process = Process.Start(startInfo))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();

                            if (!string.IsNullOrEmpty(output))
                                Logger.Instance.Info($"命令输出: {output}");
                            if (!string.IsNullOrEmpty(error))
                                Logger.Instance.Error($"命令错误: {error}");
                        }
                    }
                });

                MessageBox.Show("系统激活操作已完成，请检查激活状态！", "操作完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.Instance.Info("系统激活操作完成");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"激活过程中发生错误: {ex.Message}");
                MessageBox.Show($"激活过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ActivationSystemBtn.Enabled = true;
            }
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }



        private async void DocumentMigrationBtn_Click(object sender, EventArgs e)
        {
            try
            {
                DocumentMigrationBtn.Enabled = false;

                var migrationService = new DocumentMigrationService();
                var targetDrive = migrationService.GetTargetDrive();

                if (targetDrive == null)
                {
                    MessageBox.Show("未找到可用的目标驱动器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var result = MessageBox.Show(
                    $"将把以下文件夹迁移到 {targetDrive.Name}：\n\n" +
                    "1. 桌面\n" +
                    "2. 我的文档\n\n" +
                    "目标驱动器可用空间：" + $"{targetDrive.AvailableFreeSpace / (1024 * 1024 * 1024)}GB\n\n" +
                    "是否继续？",
                    "确认迁移",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    Logger.Instance.Info("用户取消了迁移操作");
                    return;
                }

                await migrationService.MigrateDocumentsAsync();

                MessageBox.Show("文件迁移完成！需要重启电脑生效。", "操作完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"迁移过程中发生错误: {ex.Message}");
                MessageBox.Show($"迁移过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                DocumentMigrationBtn.Enabled = true;
            }
        }

        private async void NetBtn_Click(object sender, EventArgs e)
        {
            try
            {
                NetBtn.Enabled = false;
                var progress = new Progress<string>(status => Logger.Instance.Info(status));
                var networkService = new NetworkTestService();

                Logger.Instance.Info("开始网络测试...");
                var result = await networkService.RunNetworkTestAsync(progress);

                // 显示测试结果
                var message = $"网络测试结果：\n\n" +
                             $"本地IP：{result.LocalIP}\n" +
                             $"网关地址：{result.Gateway}\n" +
                             $"外网连通性：{(result.HasInternetAccess ? "正常" : "异常")}\n\n" +
                             $"网关延迟测试（1分钟）：\n" +
                             $"平均延迟：{(result.GatewayAveragePing >= 0 ? $"{result.GatewayAveragePing:F2}ms" : "测试失败")}\n" +
                             $"丢包数：{result.GatewayLostPackets}\n\n" +
                             $"外网延迟测试（1分钟）：\n" +
                             $"平均延迟：{(result.InternetAveragePing >= 0 ? $"{result.InternetAveragePing:F2}ms" : "测试失败")}\n" +
                             $"丢包数：{result.InternetLostPackets}";

                MessageBox.Show(message, "网络测试结果", MessageBoxButtons.OK, 
                    result.HasInternetAccess ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"网络测试失败: {ex.Message}");
                MessageBox.Show($"网络测试失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                NetBtn.Enabled = true;
            }
        }
    }
}
