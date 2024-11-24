using System.Security.Principal;
using System.Diagnostics;

namespace PCAssistant
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            Logger.Initialize(LogTxtbox); // 初始化日志系统
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                Logger.Instance.Warning("程序未以管理员权限运行，部分功能可能受限");
            }

            Logger.Instance.Info("应用程序启动");

            ComputerInfo ci = new ComputerInfo();
            string info = ci.GetInfoString();
            txtComputerInfo.Text = info;

            Logger.Instance.Info("已加载系统信息");
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
    }
}
