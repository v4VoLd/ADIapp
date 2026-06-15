using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace ADIapp.Helpers
{
    public static class HardwareHelper
    {
        [DllImport("libc", CharSet = CharSet.Ansi)]
        private static extern int sysctlbyname(string name, byte[]? buffer, ref IntPtr size, IntPtr newp, IntPtr newlen);

        public static string GetCpuName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    return GetWindowsRegistryValue(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString") ?? "WIN-CPU-FALLBACK";
                }
                catch
                {
                    return "WIN-CPU-FALLBACK";
                }
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    IntPtr size = IntPtr.Zero;
                    sysctlbyname("machdep.cpu.brand_string", null, ref size, IntPtr.Zero, IntPtr.Zero);
                    if (size != IntPtr.Zero)
                    {
                        byte[] buffer = new byte[size.ToInt32()];
                        sysctlbyname("machdep.cpu.brand_string", buffer, ref size, IntPtr.Zero, IntPtr.Zero);
                        return Encoding.UTF8.GetString(buffer).Trim('\0').Trim();
                    }
                }
                catch {}
                return "MAC-CPU-FALLBACK";
            }
            return "Linux CPU";
        }

        public static string GetHddSerial()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var output = GetCmdOutput("cmd.exe", "/c wmic diskdrive get serialnumber");
                output = StripHeader(output, "SerialNumber");
                return string.IsNullOrEmpty(output) ? "WIN-DISK-SERIAL-FALLBACK" : output;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var output = GetCmdOutput("bash", "-c \"diskutil info / | grep 'Volume UUID'\"");
                if (output.Contains("Volume UUID:"))
                {
                    var parts = output.Split("Volume UUID:");
                    if (parts.Length > 1) return parts[1].Trim();
                }
                output = GetCmdOutput("bash", "-c \"ioreg -rd1 -c IOPlatformExpertDevice | grep IOPlatformUUID\"");
                if (output.Contains("IOPlatformUUID"))
                {
                    var parts = output.Split("=");
                    if (parts.Length > 1) return parts[1].Replace("\"", "").Trim();
                }
                return "MAC-DISK-UUID-FALLBACK";
            }
            return "LINUX-DISK-SERIAL-FALLBACK";
        }

        public static string GetMotherboardSerial()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    return GetWindowsRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardSerialNumber") ?? "WIN-BOARD-SERIAL-FALLBACK";
                }
                catch
                {
                    return "WIN-BOARD-SERIAL-FALLBACK";
                }
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var output = GetCmdOutput("bash", "-c \"ioreg -rd1 -c IOPlatformExpertDevice | grep IOPlatformSerialNumber\"");
                if (output.Contains("IOPlatformSerialNumber"))
                {
                    var parts = output.Split("=");
                    if (parts.Length > 1) return parts[1].Replace("\"", "").Trim();
                }
                return "MAC-BOARD-SERIAL-FALLBACK";
            }
            return "LINUX-BOARD-SERIAL-FALLBACK";
        }

        public static string GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
            return "Linux";
        }

        public static string GetVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.OSVersion.Version.ToString();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    IntPtr size = IntPtr.Zero;
                    sysctlbyname("kern.osproductversion", null, ref size, IntPtr.Zero, IntPtr.Zero);
                    if (size != IntPtr.Zero)
                    {
                        byte[] buffer = new byte[size.ToInt32()];
                        sysctlbyname("kern.osproductversion", buffer, ref size, IntPtr.Zero, IntPtr.Zero);
                        return Encoding.UTF8.GetString(buffer).Trim('\0').Trim();
                    }
                }
                catch {}
                
                var output = GetCmdOutput("sw_vers", "-productVersion");
                return string.IsNullOrEmpty(output) ? "10.15" : output;
            }
            return Environment.OSVersion.Version.ToString();
        }

        public static string GetManufacturer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    return GetWindowsRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer") ?? "WIN-MANUFACTURER-FALLBACK";
                }
                catch
                {
                    return "WIN-MANUFACTURER-FALLBACK";
                }
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Apple Inc.";
            }
            return "Linux Vendor";
        }

        public static string GetDeviceName()
        {
            return Environment.MachineName;
        }

        public static string GetDeviceId()
        {
            var raw = GetCpuName() + GetMotherboardSerial() + GetHddSerial() + GetPlatform();
            return ComputeHash(raw);
        }

        public static string GetHardwareHash()
        {
            var raw = GetCpuName() + "|" + 
                      GetHddSerial() + "|" + 
                      GetMotherboardSerial() + "|" + 
                      GetPlatform() + "|" + 
                      GetManufacturer() + "|" + 
                      GetDeviceId();
            return ComputeHash(raw);
        }

        private static string? GetWindowsRegistryValue(string subKey, string valueName)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
            return RegistryReader.Read(subKey, valueName);
        }

        private static string StripHeader(string output, string headerName)
        {
            if (string.IsNullOrEmpty(output)) return string.Empty;
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Equals(headerName, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Append(trimmed);
                }
            }
            return result.ToString().Trim();
        }

        private static string GetCmdOutput(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null) return string.Empty;
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }

#pragma warning disable CA1416
    internal static class RegistryReader
    {
        public static string? Read(string subKey, string valueName)
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subKey);
            return key?.GetValue(valueName)?.ToString();
        }
    }
#pragma warning restore CA1416
}
