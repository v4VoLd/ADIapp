using System;
using System.Diagnostics;
using System.Linq;

namespace ADIapp.Helpers;

public static class UrlHelper
{
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var escaped = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {escaped}") { CreateNoWindow = true });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", url);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open URL '{url}': {ex.Message}", ex);
            }
        }
    }

    public static void OpenMailto(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        OpenUrl($"mailto:{email.Trim()}");
    }

    public static void OpenTel(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;

        var cleaned = phone.Replace(" ", "").Replace("-", "").Trim();
        OpenUrl($"tel:{cleaned}");
    }

    public static void OpenWhatsApp(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        OpenUrl($"https://wa.me/{digits}");
    }

    public static void OpenMap(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return;

        var encoded = Uri.EscapeDataString(address.Trim());
        OpenUrl($"https://www.google.com/maps/search/?api=1&query={encoded}");
    }
}
