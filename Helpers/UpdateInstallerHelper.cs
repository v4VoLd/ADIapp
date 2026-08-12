using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ADIapp.Services;

namespace ADIapp.Helpers;

public static class UpdateInstallerHelper
{
    public static async Task<bool> DownloadAndRunInstallerAsync(string downloadUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(downloadUrl))
                return false;

            string tempDir = Path.GetTempPath();
            string extension = ".exe";

            if (OperatingSystem.IsMacOS())
                extension = ".dmg";
            else if (OperatingSystem.IsLinux())
                extension = ".deb";

            string tempFileName = $"ADIapp-Update-{Guid.NewGuid():N}{extension}";
            string tempFilePath = Path.Combine(tempDir, tempFileName);

            Logger.Info($"Downloading desktop update from {downloadUrl} to {tempFilePath}");

            using (var fileStream = File.Create(tempFilePath))
            {
                var result = await ApiService.DownloadFileToStreamAsync(downloadUrl, fileStream);
                if (!result.Success)
                {
                    Logger.Error($"Failed to download update: {result.Message}");
                    return false;
                }
            }

            Logger.Info($"Update downloaded successfully to {tempFilePath}. Launching installer...");

            LaunchInstaller(tempFilePath);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error downloading or running update installer: {ex.Message}", ex);
            return false;
        }
    }

    private static void LaunchInstaller(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };

            if (OperatingSystem.IsWindows())
            {
                // Silent execution for Inno Setup / Windows installers
                psi.Arguments = "/SILENT /NORESTART";
            }
            else if (OperatingSystem.IsMacOS())
            {
                psi.FileName = "open";
                psi.Arguments = $"\"{filePath}\"";
            }
            else if (OperatingSystem.IsLinux())
            {
                psi.FileName = "xdg-open";
                psi.Arguments = $"\"{filePath}\"";
            }

            Process.Start(psi);

            // Shutdown running desktop app process to allow setup to overwrite files
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error executing installer process {filePath}: {ex.Message}", ex);
        }
    }
}
