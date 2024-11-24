using System.Collections.Concurrent;

namespace PCAssistant
{
    public class Logger
    {
        private static Logger? _instance;
        private static readonly object _lock = new object();
        private readonly TextBox? _logTextBox;
        private readonly string _logPath;
        private readonly ConcurrentQueue<LogMessage> _messageQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processTask;

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Debug
        }

        private class LogMessage
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; }

            public LogMessage(LogLevel level, string message)
            {
                Timestamp = DateTime.Now;
                Level = level;
                Message = message;
            }

            public override string ToString()
            {
                return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
            }
        }

        private Logger(TextBox? logTextBox = null)
        {
            _logTextBox = logTextBox;
            _messageQueue = new ConcurrentQueue<LogMessage>();
            _cancellationTokenSource = new CancellationTokenSource();

            // 创建日志目录
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logDir = Path.Combine(baseDir, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // 创建日志文件（按日期）
            string fileName = $"Log_{DateTime.Now:yyyy-MM-dd}.txt";
            _logPath = Path.Combine(logDir, fileName);

            // 启动处理消息的后台任务
            _processTask = Task.Run(ProcessLogMessages, _cancellationTokenSource.Token);
        }

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new Logger();
                    }
                }
                return _instance;
            }
        }

        public static void Initialize(TextBox logTextBox)
        {
            lock (_lock)
            {
                _instance = new Logger(logTextBox);
            }
        }

        public void Log(LogLevel level, string message)
        {
            var logMessage = new LogMessage(level, message);
            _messageQueue.Enqueue(logMessage);
        }

        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);

        private async Task ProcessLogMessages()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                while (_messageQueue.TryDequeue(out LogMessage? message))
                {
                    string logText = message.ToString();

                    // 写入文件
                    try
                    {
                        await File.AppendAllTextAsync(_logPath, logText + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        // 如果写入文件失败，至少尝试显示在TextBox中
                        System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                    }

                    // 更新UI
                    if (_logTextBox != null)
                    {
                        try
                        {
                            if (_logTextBox.InvokeRequired)
                            {
                                _logTextBox.Invoke(new Action(() => UpdateTextBox(logText)));
                            }
                            else
                            {
                                UpdateTextBox(logText);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to update TextBox: {ex.Message}");
                        }
                    }
                }

                await Task.Delay(100); // 避免CPU占用过高
            }
        }

        private void UpdateTextBox(string logText)
        {
            if (_logTextBox == null) return;

            if (_logTextBox.TextLength > 50000) // 防止TextBox内容过多
            {
                _logTextBox.Clear();
            }

            _logTextBox.AppendText(logText + Environment.NewLine);
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _processTask.Wait(1000); // 等待最后的日志处理完成
            _cancellationTokenSource.Dispose();
        }
    }
} 