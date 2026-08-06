using System;
using System.Net.NetworkInformation;

namespace ADIapp.Helpers;

public static class NetworkHelper
{
    public static bool IsNetworkAvailable()
    {
        try
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
        catch (Exception ex)
        {
            Logger.Error($"Network availability check error: {ex.Message}", ex);
            return true; // Fallback to true if interface check throws
        }
    }
}
