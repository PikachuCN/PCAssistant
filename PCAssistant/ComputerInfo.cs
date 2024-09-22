using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
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
            return GetWMIInfo("Win32_OperatingSystem", "Version");
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

        public void PrintInfo()
        {
            Console.WriteLine($"CPU: {CPU}");
            Console.WriteLine($"GPU: {GPU}");
            Console.WriteLine($"Memory: {Memory}");
            Console.WriteLine($"Motherboard: {Motherboard}");
            Console.WriteLine($"BIOS Version: {BIOSVersion}");
            Console.WriteLine($"Operating System Version: {OSVersion}");
            Console.WriteLine("Disk Drives:");
            foreach (var disk in DiskDrives)
            {
                Console.WriteLine($"  - Model: {disk.Model}");
                Console.WriteLine($"    Serial Number: {disk.SerialNumber}");
                Console.WriteLine($"    Size: {disk.Size} GB");
                foreach (var partition in disk.Partitions)
                {
                    Console.WriteLine($"    Partition: {partition.Name}");
                    Console.WriteLine($"      Capacity: {partition.Capacity} GB");
                    Console.WriteLine($"      Free Space: {partition.FreeSpace} GB");
                }
            }
            Console.WriteLine("Network Interfaces:");
            foreach (var nic in NetworkInterfaces)
            {
                Console.WriteLine($"  - Name: {nic.Name}");
                Console.WriteLine($"    MAC Address: {nic.MACAddress}");
                Console.WriteLine($"    IP Addresses: {string.Join(", ", nic.IPAddresses)}");
            }
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
