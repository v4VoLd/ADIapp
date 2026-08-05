using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ADIapp.Helpers
{
    public static class VmDetector
    {
        [DllImport("libc", CharSet = CharSet.Ansi)]
        private static extern int sysctlbyname(string name, byte[]? buffer, ref IntPtr size, IntPtr newp, IntPtr newlen);

        private static readonly string[] VmKeywords = new[]
        {
            "vmware",
            "virtualbox",
            "vbox",
            "qemu",
            "kvm",
            "xen",
            "hyper-v",
            "hyperv",
            "parallels",
            "virtual machine",
            "innotek",
            "bochs",
            "bhyve",
            "sandbox"
        };

        public static bool IsVirtualMachine()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return IsWindowsVirtualMachine();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return IsMacVirtualMachine();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return IsLinuxVirtualMachine();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"VM detection error: {ex.Message}", ex);
            }
            return false;
        }

        private static bool IsWindowsVirtualMachine()
        {
            string manufacturer = RegistryReader.Read(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer") ?? "";
            string productName = RegistryReader.Read(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName") ?? "";
            string biosVersion = RegistryReader.Read(@"HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion") ?? "";
            string cpuName = RegistryReader.Read(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString") ?? "";

            string combined = $"{manufacturer} {productName} {biosVersion} {cpuName}".ToLowerInvariant();

            foreach (var kw in VmKeywords)
            {
                if (combined.Contains(kw)) return true;
            }

            string wmiManufacturer = GetCmdOutput("cmd.exe", "/c wmic computersystem get manufacturer");
            string wmiModel = GetCmdOutput("cmd.exe", "/c wmic computersystem get model");
            string wmiCombined = $"{wmiManufacturer} {wmiModel}".ToLowerInvariant();

            foreach (var kw in VmKeywords)
            {
                if (wmiCombined.Contains(kw)) return true;
            }

            return false;
        }

        private static bool IsMacVirtualMachine()
        {
            try
            {
                IntPtr size = (IntPtr)sizeof(int);
                byte[] buffer = new byte[sizeof(int)];
                if (sysctlbyname("kern.hv_vmm_present", buffer, ref size, IntPtr.Zero, IntPtr.Zero) == 0)
                {
                    int vmmPresent = BitConverter.ToInt32(buffer, 0);
                    if (vmmPresent != 0) return true;
                }
            }
            catch {}

            string macModel = GetCmdOutput("sysctl", "-n hw.model").ToLowerInvariant();
            foreach (var kw in VmKeywords)
            {
                if (macModel.Contains(kw)) return true;
            }

            string cpuBrand = HardwareHelper.GetCpuName().ToLowerInvariant();
            foreach (var kw in VmKeywords)
            {
                if (cpuBrand.Contains(kw)) return true;
            }

            string ioregOutput = GetCmdOutput("bash", "-c \"ioreg -rd1 -c IOPlatformExpertDevice\"").ToLowerInvariant();
            if (ioregOutput.Contains("virtualbox") || ioregOutput.Contains("vmware") || ioregOutput.Contains("parallels") || ioregOutput.Contains("qemu"))
            {
                return true;
            }

            return false;
        }

        private static bool IsLinuxVirtualMachine()
        {
            string virtType = GetCmdOutput("systemd-detect-virt", "").ToLowerInvariant();
            if (!string.IsNullOrEmpty(virtType) && !virtType.Contains("none"))
            {
                return true;
            }

            try
            {
                if (File.Exists("/sys/class/dmi/id/product_name"))
                {
                    string product = File.ReadAllText("/sys/class/dmi/id/product_name").ToLowerInvariant();
                    foreach (var kw in VmKeywords)
                    {
                        if (product.Contains(kw)) return true;
                    }
                }
                if (File.Exists("/sys/class/dmi/id/sys_vendor"))
                {
                    string vendor = File.ReadAllText("/sys/class/dmi/id/sys_vendor").ToLowerInvariant();
                    foreach (var kw in VmKeywords)
                    {
                        if (vendor.Contains(kw)) return true;
                    }
                }
            }
            catch {}

            return false;
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
    }
}
