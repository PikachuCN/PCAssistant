using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Microsoft.Win32;
namespace PCAssistant
{
    public class ComputerInfo
    {
        public string CPU { get; private set; }
        public string GPU { get; private set; }
        public string Memory { get; private set; }
        public string Motherboard { get; private set; }
        public string BIOSVersion { get; private set; }
        public List<DiskDriveInfo> DiskDrives { get; private set; }
        public List<NetworkInterfaceInfo> NetworkInterfaces { get; private set; }
        public string OSVersion { get; private set; }

        public ComputerInfo()
        {
            CPU = GetCPUInfo();
            GPU = GetGPUInfo();
            Memory = GetMemoryInfo();
            Motherboard = GetMotherboardInfo();
            BIOSVersion = GetBIOSVersion();
            DiskDrives = GetDiskDrivesInfo();
            NetworkInterfaces = GetNetworkInterfacesInfo();
            OSVersion = GetOSVersion();
        }

        private string GetCPUInfo()
        {
            return GetWMIInfo("Win32_Processor", "Name");
        }

        private string GetGPUInfo()
        {
            return GetWMIInfo("Win32_VideoController", "Name");
        }

        private string GetMemoryInfo()
        {
            long totalCapacity = 0;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                totalCapacity += Convert.ToInt64(obj["Capacity"]);
            }
            return $"{totalCapacity / (1024 * 1024 * 1024)} GB";
        }

        private string GetMotherboardInfo()
        {
            return GetWMIInfo("Win32_BaseBoard", "Product");
        }

        private string GetBIOSVersion()
        {
            return GetWMIInfo("Win32_BIOS", "Version");
        }

        private string GetOSVersion()
        {
            string version = GetWMIInfo("Win32_OperatingSystem", "Version");
            string buildNumber = GetWMIInfo("Win32_OperatingSystem", "BuildNumber");

            // 从注册表获取 DisplayVersion 或 ReleaseId
            string detailedVersion = GetWindowsDetailedVersion();

            if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(buildNumber))
            {
                string[] versionParts = version.Split('.');
                int majorVersion = int.Parse(versionParts[0]);

                if (majorVersion == 10)
                {
                    int buildNum = int.Parse(buildNumber);
                    if (buildNum >= 22000)
                    {
                        return $"Windows 11 {detailedVersion} (Build {buildNumber})";
                    }
                    return $"Windows 10 {detailedVersion} (Build {buildNumber})";
                }
                else if (majorVersion == 6)
                {
                    int minorVersion = int.Parse(versionParts[1]);
                    if (minorVersion == 1)
                        return $"Windows 7 (Build {buildNumber})";
                    if (minorVersion == 2)
                        return $"Windows 8 (Build {buildNumber})";
                    if (minorVersion == 3)
                        return $"Windows 8.1 (Build {buildNumber})";
                }

                return $"Unknown Windows version ({version}, Build {buildNumber})";
            }

            return "Unknown Windows version";
        }


        private string GetWindowsDetailedVersion()
        {
            string detailedVersion = "";

            try
            {
                // 打开注册表项
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        // 尝试获取 DisplayVersion 或 ReleaseId
                        object displayVersion = key.GetValue("DisplayVersion");
                        object releaseId = key.GetValue("ReleaseId");

                        if (displayVersion != null)
                        {
                            detailedVersion = displayVersion.ToString();
                        }
                        else if (releaseId != null)
                        {
                            detailedVersion = releaseId.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                detailedVersion = "Unknown Version";
                // 你可以在这里记录异常信息
            }

            return detailedVersion;
        }

        private List<DiskDriveInfo> GetDiskDrivesInfo()
        {
            List<DiskDriveInfo> diskDrives = new List<DiskDriveInfo>();

            // 获取物理磁盘信息
            ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("select * from Win32_DiskDrive");
            foreach (ManagementObject disk in diskSearcher.Get())
            {
                DiskDriveInfo diskDriveInfo = new DiskDriveInfo
                {
                    Model = disk["Model"]?.ToString(),
                    SerialNumber = disk["SerialNumber"]?.ToString(),
                    Size = Convert.ToInt64(disk["Size"]) / (1024 * 1024 * 1024) // 转换为 GB
                };

                // 获取该磁盘的分区信息
                ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{disk["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    // 获取分区对应的逻辑磁盘信息
                    ManagementObjectSearcher logicalSearcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                    foreach (ManagementObject logical in logicalSearcher.Get())
                    {
                        PartitionInfo partitionInfo = new PartitionInfo
                        {
                            Name = logical["Name"]?.ToString(),
                            Capacity = Convert.ToInt64(logical["Size"]) / (1024 * 1024 * 1024), // 转换为 GB
                            FreeSpace = Convert.ToInt64(logical["FreeSpace"]) / (1024 * 1024 * 1024) // 转换为 GB
                        };
                        diskDriveInfo.Partitions.Add(partitionInfo);
                    }
                }

                diskDrives.Add(diskDriveInfo);
            }

            return diskDrives;
        }

        private List<NetworkInterfaceInfo> GetNetworkInterfacesInfo()
        {
            List<NetworkInterfaceInfo> networkInterfaces = new List<NetworkInterfaceInfo>();

            foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && nic.GetPhysicalAddress().ToString() != "")
                {
                    NetworkInterfaceInfo networkInterfaceInfo = new NetworkInterfaceInfo
                    {
                        Name = nic.Name,
                        MACAddress = nic.GetPhysicalAddress().ToString()
                    };

                    foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        networkInterfaceInfo.IPAddresses.Add(ip.Address.ToString());
                    }

                    networkInterfaces.Add(networkInterfaceInfo);
                }
            }

            return networkInterfaces;
        }

        private string GetWMIInfo(string className, string propertyName, string condition = null)
        {
            string info = "";
            string query = $"select {propertyName} from {className}";
            if (!string.IsNullOrEmpty(condition))
            {
                query += $" where {condition}";
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
            {
                info = obj[propertyName]?.ToString();
            }
            return info;
        }


        public string GetInfoString()
        {
            StringBuilder infoBuilder = new StringBuilder();

            infoBuilder.AppendLine($"CPU: {CPU}");
            infoBuilder.AppendLine($"显卡: {GPU}");
            infoBuilder.AppendLine($"内存: {Memory}");
            infoBuilder.AppendLine($"主板信息: {Motherboard}");
            infoBuilder.AppendLine($"BIOS 版本: {BIOSVersion}");
            infoBuilder.AppendLine($"操作系统: {OSVersion}");

            infoBuilder.AppendLine("磁盘信息:");
            foreach (var disk in DiskDrives)
            {
                infoBuilder.AppendLine($"  - 型号: {disk.Model}");
                infoBuilder.AppendLine($"    磁盘序列号: {disk.SerialNumber}");
                infoBuilder.AppendLine($"    总大小: {disk.Size} GB");
                foreach (var partition in disk.Partitions)
                {
                    infoBuilder.AppendLine($"    分区: {partition.Name}");
                    infoBuilder.AppendLine($"      总容量: {partition.Capacity} GB");
                    infoBuilder.AppendLine($"      可用空间e: {partition.FreeSpace} GB");
                }
            }

            infoBuilder.AppendLine("网络信息:");
            foreach (var nic in NetworkInterfaces)
            {
                infoBuilder.AppendLine($"  - 网卡名字: {nic.Name}");
                infoBuilder.AppendLine($"    MAC 地址: {nic.MACAddress}");
                infoBuilder.AppendLine($"    IP 地址: {string.Join(", ", nic.IPAddresses)}");
            }

            return infoBuilder.ToString();
        }
    }

    public class DiskDriveInfo
    {
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public long Size { get; set; } // 硬盘总容量 (GB)
        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>(); // 分区信息
    }

    public class PartitionInfo
    {
        public string Name { get; set; }
        public long Capacity { get; set; } // 分区总容量 (GB)
        public long FreeSpace { get; set; } // 分区可用空间 (GB)
    }

    public class NetworkInterfaceInfo
    {
        public string Name { get; set; }
        public string MACAddress { get; set; }
        public List<string> IPAddresses { get; set; } = new List<string>();
    }
}
