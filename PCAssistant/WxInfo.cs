using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace PCAssistant
{
    public class WxInfo
    {
        // Windows API 常量
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_GUARD = 0x100;
        private const uint PAGE_NOACCESS = 0x01;
        private const uint PAGE_READONLY = 0x02;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_WRITECOPY = 0x08;
        private const uint PAGE_EXECUTE = 0x10;
        private const uint PAGE_EXECUTE_READ = 0x20;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;

        // Windows API 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        // 额外需要的Windows API
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer,
            uint dwLength);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        // 用于存储微信信息的类
        public class WeChatData
        {
            public int Pid { get; set; }
            public string Wxid { get; set; }
            public string FilePath { get; set; }
            public string Key { get; set; }
        }

        // 获取PE文件位数
        public static int GetExeBit(string filePath)
        {
            try
            {
                // 使用二进制方式打开文件
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    // 检查DOS头
                    if (reader.ReadInt16() != 0x5A4D) // "MZ"
                    {
                        Console.WriteLine("get exe bit error: Invalid PE file");
                        return 64;
                    }

                    // 定位到PE头位置
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = reader.ReadInt32();

                    // 移动到PE头
                    fs.Seek(peOffset, SeekOrigin.Begin);

                    // 验证PE签名 "PE\0\0"
                    if (reader.ReadInt32() != 0x00004550)
                    {
                        Console.WriteLine("get exe bit error: Invalid PE signature");
                        return 64;
                    }

                    // 读取Machine字段
                    ushort machine = reader.ReadUInt16();

                    switch (machine)
                    {
                        case 0x14c:  // IMAGE_FILE_MACHINE_I386
                            return 32;
                        case 0x8664: // IMAGE_FILE_MACHINE_AMD64
                            return 64;
                        case 0x0200: // IMAGE_FILE_MACHINE_IA64
                            return 64;
                        default:
                            Console.WriteLine($"get exe bit error: Unknown architecture: 0x{machine:X4}");
                            return 64;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"get exe bit error: {ex.Message}");
                return 64;
            }
        }

        // 模式扫描
        private static List<IntPtr> PatternScanAll(IntPtr handle, byte[] pattern, int findNum = 100, string moduleName = null)
        {
            List<IntPtr> found = new List<IntPtr>();

            try
            {
                // 如果指定了模块名，获取模块的地址范围
                long startAddress = 0;
                long endAddress = 0x7FFFFFFF0000;

                if (!string.IsNullOrEmpty(moduleName))
                {
                    // 使用GetProcessId获取正确的进程ID
                    int processId = GetProcessId(handle);
                    if (processId == 0)
                    {
                        Console.WriteLine("Failed to get process ID");
                        return found;
                    }

                    var process = Process.GetProcessById(processId);
                    var module = process.Modules.Cast<ProcessModule>()
                        .FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

                    if (module != null)
                    {
                        startAddress = (long)module.BaseAddress;
                        endAddress = startAddress + module.ModuleMemorySize;
                        Console.WriteLine($"Scanning module {moduleName} from 0x{startAddress:X} to 0x{endAddress:X}");
                    }
                    else
                    {
                        Console.WriteLine($"Module {moduleName} not found");
                        return found;
                    }
                }

                long nextRegion = startAddress;
                while (nextRegion < endAddress)
                {
                    MEMORY_BASIC_INFORMATION mbi = new MEMORY_BASIC_INFORMATION();
                    if (VirtualQueryEx(handle, (IntPtr)nextRegion, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                        break;

                    if (mbi.State == MEM_COMMIT &&
                        mbi.Protect != PAGE_NOACCESS &&
                        (mbi.Protect & PAGE_GUARD) == 0)
                    {
                        try
                        {
                            long regionSize = mbi.RegionSize.ToInt64();
                            if (regionSize > 0 && regionSize <= 100 * 1024 * 1024)
                            {
                                byte[] buffer = new byte[(int)regionSize];
                                IntPtr bytesRead;
                                if (ReadProcessMemory(handle, mbi.BaseAddress, buffer, buffer.Length, out bytesRead))
                                {
                                    for (int i = 0; i <= buffer.Length - pattern.Length; i++)
                                    {
                                        if (buffer[i] == pattern[0])
                                        {
                                            bool match = true;
                                            for (int j = 1; j < pattern.Length; j++)
                                            {
                                                if (buffer[i + j] != pattern[j])
                                                {
                                                    match = false;
                                                    break;
                                                }
                                            }
                                            if (match)
                                            {
                                                found.Add((IntPtr)((long)mbi.BaseAddress + i));
                                                if (found.Count >= findNum)
                                                    return found;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error scanning region at {mbi.BaseAddress:X}: {ex.Message}");
                        }
                    }
                    nextRegion = (long)mbi.BaseAddress + (long)mbi.RegionSize;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PatternScanAll error: {ex.Message}");
            }
            return found;
        }

        // 获取微信ID
        private static string GetWxId(IntPtr hProcess)
        {
            byte[] pattern = Encoding.ASCII.GetBytes(@"\Msg\FTSContact");
            var addrs = PatternScanAll(hProcess, pattern, 100);

            List<string> wxids = new List<string>();
            foreach (var addr in addrs)
            {
                byte[] buffer = new byte[80];
                IntPtr bytesRead;
                if (ReadProcessMemory(hProcess, (IntPtr)((long)addr - 30), buffer, buffer.Length, out bytesRead))
                {
                    try
                    {
                        // 过滤掉无效的符串
                        string data = Encoding.UTF8.GetString(buffer)
                            .TrimEnd('\0')  // 移除空字符
                            .Replace("\0", ""); // 移除中间的空字符

                        string[] parts = data.Split(new[] { @"\Msg" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            string[] subParts = parts[0].Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                            if (subParts.Length > 0)
                            {
                                string wxid = subParts[subParts.Length - 1].Trim();
                                if (!string.IsNullOrWhiteSpace(wxid) && wxid.Length > 0)
                                {
                                    wxids.Add(wxid);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 忽略解析错误
                        continue;
                    }
                }
            }

            // 使用更安全的方式找出最常见的wxid
            if (wxids.Count > 0)
            {
                try
                {
                    var groups = wxids
                        .Where(x => !string.IsNullOrWhiteSpace(x))  // 确保没有空值
                        .GroupBy(x => x)
                        .Select(g => new { Wxid = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    if (groups.Any())
                    {
                        return groups.First().Wxid;
                    }
                }
                catch
                {
                    // 如果分组失败，返回第一个有效的wxid
                    return wxids.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "None";
                }
            }

            return "None";
        }

        // 主要的读取信息方法
        public static async Task<object> ReadInfoAsync(bool isLogging = false, bool isSave = false)
        {
            return await Task.Run(() =>
            {
                var result = new List<WeChatData>();
                var processes = Process.GetProcessesByName("WeChat");

                if (!processes.Any())
                {
                    string error = "[-] WeChat No Run";
                    if (isLogging) Console.WriteLine(error);
                    return result;
                }

                foreach (var process in processes)
                {
                    try
                    {
                        var data = new WeChatData
                        {
                            Pid = process.Id
                        };

                        IntPtr handle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
                        if (handle == IntPtr.Zero)
                        {
                            if (isLogging) Console.WriteLine($"[-] Failed to open process {process.Id}");
                            continue;
                        }

                        int addrLen = GetExeBit(process.MainModule.FileName) / 8;

                        data.Wxid = GetWxId(handle);
                        if (data.Wxid != "None")
                        {
                            data.FilePath = GetInfoFilePath(data.Wxid);
                            if (data.FilePath != "None")
                            {
                                data.Key = GetKey(process.Id, data.FilePath, addrLen);
                            }
                        }

                        result.Add(data);
                    }
                    catch (Exception ex)
                    {
                        if (isLogging) Console.WriteLine($"[-] Error processing WeChat process {process.Id}: {ex.Message}");
                    }
                }

                if (isLogging)
                {
                    Console.WriteLine("================================");
                    foreach (var item in result)
                    {
                        Console.WriteLine($"[+]     Pid: {item.Pid}");
                        Console.WriteLine($"[+]    Wxid: {item.Wxid}");
                        Console.WriteLine($"[+] FilePath: {item.FilePath}");
                        Console.WriteLine($"[+]     Key: {item.Key}");
                        Console.WriteLine("--------------------------------");
                    }
                    Console.WriteLine("================================");
                }

                //if (isSave)
                //{
                //    var options = new JsonSerializerOptions
                //    {
                //        WriteIndented = true,
                //        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                //    };
                //    File.WriteAllText("wx_info.txt", JsonSerializer.Serialize(result, options));
                //}

                return result;
            });
        }

        private static string GetInfoFilePath(string wxid = "all")
        {
            if (string.IsNullOrEmpty(wxid))
                return "None";

            string wDir = "MyDocument:";
            bool isWDir = false;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Tencent\WeChat"))
                {
                    if (key != null)
                    {
                        string value = key.GetValue("FileSavePath") as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            wDir = value;
                            isWDir = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                wDir = "MyDocument:";
            }

            if (!isWDir)
            {
                try
                {
                    string userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                    string path3ebffe94 = Path.Combine(userProfile, "AppData", "Roaming", "Tencent", "WeChat", "All Users", "config", "3ebffe94.ini");
                    if (File.Exists(path3ebffe94))
                    {
                        wDir = File.ReadAllText(path3ebffe94, Encoding.UTF8);
                        isWDir = true;
                    }
                }
                catch (Exception)
                {
                    wDir = "MyDocument:";
                }
            }

            if (wDir == "MyDocument:")
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
                    {
                        if (key != null)
                        {
                            string documentsPath = key.GetValue("Personal") as string;
                            if (!string.IsNullOrEmpty(documentsPath))
                            {
                                string[] documentsPaths = documentsPath.Split(Path.DirectorySeparatorChar);
                                if (documentsPath.Contains('%'))
                                {
                                    string envVar = documentsPaths[0].Trim('%');
                                    wDir = Environment.GetEnvironmentVariable(envVar);
                                    wDir = Path.Combine(wDir, Path.Combine(documentsPaths.Skip(1).ToArray()));
                                }
                                else
                                {
                                    wDir = documentsPath;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    string profile = Environment.GetEnvironmentVariable("USERPROFILE");
                    wDir = Path.Combine(profile, "Documents");
                }
            }

            string msgDir = Path.Combine(wDir, "WeChat Files");

            if (wxid == "all" && Directory.Exists(msgDir))
                return msgDir;

            string filePath = Path.Combine(msgDir, wxid);
            return Directory.Exists(filePath) ? filePath : "None";
        }

        private static string GetKey(int pid, string dbPath, int addrLen)
        {
            byte[] ReadKeyBytes(IntPtr hProcess, IntPtr address, int addressLen = 8)
            {
                try
                {
                    // 第一次读取：读取指针
                    byte[] array = new byte[addressLen];
                    IntPtr bytesRead;
                    if (!ReadProcessMemory(hProcess, address, array, addressLen, out bytesRead))
                    {
                        return null;
                    }

                    // 将字节数组转换为地址值（与Python代码完全一致）
                    long keyAddress = 0;
                    for (int i = 0; i < addressLen; i++)
                    {
                        keyAddress |= ((long)array[i] << (i * 8));
                    }

                    // 第二次读取：读取实际的key
                    byte[] key = new byte[32];
                    if (!ReadProcessMemory(hProcess, (IntPtr)keyAddress, key, 32, out bytesRead))
                    {
                        return null;
                    }

                    return key;
                }
                catch
                {
                    return null;
                }
            }

            bool VerifyKey(byte[] key, string wxDbPath)
            {
                try
                {
                    if (!File.Exists(wxDbPath))
                    {
                        Console.WriteLine("Database file not found");
                        return false;
                    }

                    // 读取数据库文件的前5000字节
                    byte[] blist;
                    using (var fs = new FileStream(wxDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        blist = new byte[5000];
                        int bytesRead = fs.Read(blist, 0, blist.Length);
                        if (bytesRead < 4096)
                        {
                            Console.WriteLine($"Read only {bytesRead} bytes from database");
                            return false;
                        }
                    }

                    byte[] salt = new byte[16];
                    Array.Copy(blist, 0, salt, 0, 16);

                    using (var pbkdf2 = new Rfc2898DeriveBytes(key, salt, 64000, HashAlgorithmName.SHA1))
                    {
                        byte[] byteKey = pbkdf2.GetBytes(32);
                        byte[] first = new byte[4096 - 16];
                        Array.Copy(blist, 16, first, 0, 4096 - 16);

                        // 生成mac_salt
                        byte[] macSalt = salt.Select(b => (byte)(b ^ 58)).ToArray();

                        using (var pbkdf2Mac = new Rfc2898DeriveBytes(byteKey, macSalt, 2, HashAlgorithmName.SHA1))
                        {
                            byte[] macKey = pbkdf2Mac.GetBytes(32);
                            using (var hmac = new HMACSHA1(macKey))
                            {
                                byte[] hashData = first.Take(first.Length - 32).ToArray();
                                hmac.TransformBlock(hashData, 0, hashData.Length, null, 0);
                                hmac.TransformFinalBlock(new byte[] { 1, 0, 0, 0 }, 0, 4);

                                byte[] computedHash = hmac.Hash;
                                byte[] expectedHash = first.Skip(first.Length - 32).Take(20).ToArray();

                                bool isMatch = computedHash.Take(20).SequenceEqual(expectedHash);
                                if (isMatch)
                                {
                                    Console.WriteLine("Key verification successful!");
                                }
                                return isMatch;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VerifyKey error: {ex.Message}");
                    return false;
                }
            }

            try
            {
                string[] phoneTypes = { "iphone\0", "android\0", "ipad\0" };
                IntPtr handle = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
                if (handle == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to open process {pid}");
                    return "None";
                }

                string microMsgPath = Path.Combine(dbPath, "MSG", "MicroMsg.db");
                Console.WriteLine($"Scanning process {pid} for key...");

                List<IntPtr> allAddresses = new List<IntPtr>();
                foreach (string phoneType in phoneTypes)
                {
                    var addrs = PatternScanAll(handle, Encoding.ASCII.GetBytes(phoneType), 10, "WeChatWin.dll");
                    if (addrs != null && addrs.Count >= 2)
                    {
                        Console.WriteLine($"Found {addrs.Count} addresses for {phoneType}");
                        allAddresses.AddRange(addrs);
                    }
                }

                if (allAddresses.Count == 0)
                {
                    Console.WriteLine("No pattern addresses found");
                    return "None";
                }

                allAddresses = allAddresses.OrderByDescending(x => (long)x).ToList();
                Console.WriteLine($"Total addresses to scan: {allAddresses.Count}");

                foreach (var addr in allAddresses)
                {
                    Console.WriteLine($"Scanning address range: 0x{addr.ToInt64():X} to 0x{addr.ToInt64() - 2000:X}");
                    for (long j = (long)addr; j > (long)addr - 2000; j -= addrLen)
                    {
                        if (j % addrLen != 0) continue;

                        byte[] keyBytes = ReadKeyBytes(handle, (IntPtr)j, addrLen);
                        if (keyBytes == null)
                            continue;

                        // 打印每个可能的key的前几个字节
                        Console.WriteLine($"Testing potential key at 0x{j:X}: {BitConverter.ToString(keyBytes.Take(4).ToArray())}...");

                        if (VerifyKey(keyBytes, microMsgPath))
                        {
                            string key = BitConverter.ToString(keyBytes).Replace("-", "").ToLower();
                            Console.WriteLine($"Found valid key: {key}");
                            return key;
                        }
                    }
                }
                CloseHandle(handle);
                Console.WriteLine("No valid key found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetKey error: {ex.Message}");
            }

            // 记得关闭handle


            return "None";
        }
    }
}
