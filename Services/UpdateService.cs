using System;
using System.Threading.Tasks;
using ADIapp.Config;
using ADIapp.Helpers;
using ADIapp.Models;

namespace ADIapp.Services;

public static class UpdateService
{
    private static bool _isChecking = false;

    public static async Task CheckAndPerformUpdateAsync()
    {
        if (_isChecking) return;
        _isChecking = true;

        try
        {
            string currentVersion = AppConfig.AppVersion;
            string platform = HardwareHelper.GetPlatform();

            Logger.Info($"Checking for desktop application updates. Current version: {currentVersion}, Platform: {platform}");

            var updateResponse = await ApiService.CheckForUpdatesAsync(currentVersion, platform);

            if (updateResponse == null || !updateResponse.Success || updateResponse.Data == null)
            {
                Logger.Info("No update response or version check failed.");
                return;
            }

            var data = updateResponse.Data;

            if (data.HasUpdate && !string.IsNullOrEmpty(data.DownloadUrl))
            {
                Logger.Info($"New desktop version available: {data.LatestVersion} (Mandatory: {data.Mandatory}). Initiating silent background update...");
                await UpdateInstallerHelper.DownloadAndRunInstallerAsync(data.DownloadUrl);
            }
            else
            {
                Logger.Info($"Desktop app is up to date (version {currentVersion}).");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during desktop update check: {ex.Message}", ex);
        }
        finally
        {
            _isChecking = false;
        }
    }
}
