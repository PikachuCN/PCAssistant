using Microsoft.Win32;

namespace PCAssistant.Services
{
    public class DocumentMigrationService
    {
        private readonly Logger _logger;

        public DocumentMigrationService()
        {
            _logger = Logger.Instance;
        }

        public async Task MigrateDocumentsAsync()
        {
            _logger.Info("开始检查文档迁移...");
            
            // 获取目标驱动器
            var targetDrive = GetTargetDrive();
            if (targetDrive == null) return;

            _logger.Info($"找到目标驱动器 {targetDrive.Name}，可用空间: {targetDrive.AvailableFreeSpace / (1024 * 1024 * 1024)}GB");

            // 获取路径
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string targetBasePath = Path.Combine(targetDrive.Name, "UserFiles");
            string targetDesktopPath = Path.Combine(targetBasePath, "Desktop");
            string targetDocumentsPath = Path.Combine(targetBasePath, "Documents");

            // 创建目标目录
            Directory.CreateDirectory(targetBasePath);
            Directory.CreateDirectory(targetDesktopPath);
            Directory.CreateDirectory(targetDocumentsPath);

            // 执行迁移
            await Task.Run(() =>
            {
                // 迁移桌面文件
                _logger.Info("开始迁移桌面文件...");
                MoveDirectory(desktopPath, targetDesktopPath);

                // 迁移文档
                _logger.Info("开始迁移我的文档...");
                MoveDirectory(documentsPath, targetDocumentsPath);

                // 更新注册表
                UpdateRegistryPaths(targetDesktopPath, targetDocumentsPath);
            });

            _logger.Info("文件迁移完成");
        }

        public DriveInfo? GetTargetDrive()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed && d.Name.ToUpper()[0] != 'C')
                .OrderByDescending(d => d.AvailableFreeSpace)
                .ToList();

            if (!drives.Any())
            {
                _logger.Error("未找到可用的目标驱动器");
                return null;
            }

            return drives.First();
        }

        private void UpdateRegistryPaths(string desktopPath, string documentsPath)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", true);
                
                if (key != null)
                {
                    key.SetValue("Desktop", desktopPath, RegistryValueKind.ExpandString);
                    key.SetValue("Personal", documentsPath, RegistryValueKind.ExpandString);
                    _logger.Info("已更新注册表路径");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"更新注册表失败: {ex.Message}");
                throw;
            }
        }

        private void MoveDirectory(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);

            // 处理文件
            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(file);
                string targetFile = Path.Combine(targetPath, fileName);
                MoveFile(file, targetFile, fileName);
            }

            // 处理子目录
            foreach (string dir in Directory.GetDirectories(sourcePath))
            {
                string dirName = Path.GetFileName(dir);
                string targetDir = Path.Combine(targetPath, dirName);
                ProcessSubDirectory(dir, targetDir, dirName);
            }
        }

        private void MoveFile(string sourcePath, string targetPath, string fileName)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    _logger.Warning($"目标文件已存在，跳过: {fileName}");
                    return;
                }

                File.Copy(sourcePath, targetPath, false);
                _logger.Info($"已复制文件: {fileName}");

                try
                {
                    File.Delete(sourcePath);
                    _logger.Info($"已删除源文件: {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.Warning($"删除源文件 {fileName} 失败，可能被占用: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"处理文件 {fileName} 时出错: {ex.Message}");
            }
        }

        private void ProcessSubDirectory(string sourcePath, string targetPath, string dirName)
        {
            try
            {
                MoveDirectory(sourcePath, targetPath);
                
                if (!Directory.EnumerateFileSystemEntries(sourcePath).Any())
                {
                    try
                    {
                        Directory.Delete(sourcePath, false);
                        _logger.Info($"已删除空目录: {dirName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"删除空目录 {dirName} 失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"处理目录 {dirName} 时出错: {ex.Message}");
            }
        }
    }
} 