using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace ADIapp.Services;

public static class BrandLogoService
{
    private static readonly ConcurrentDictionary<string, Bitmap> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly string _diskCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ADIapp",
        "Cache",
        "Brands"
    );

    private static Bitmap? _defaultFallbackLogo;

    public static Bitmap DefaultLogo
    {
        get
        {
            if (_defaultFallbackLogo == null)
            {
                try
                {
                    _defaultFallbackLogo = new Bitmap(AssetLoader.Open(new Uri("avares://ADIapp/Assets/sidebar_logo.png")));
                }
                catch
                {
                    // Fallback to null if asset missing
                }
            }
            return _defaultFallbackLogo!;
        }
    }

    static BrandLogoService()
    {
        try
        {
            if (!Directory.Exists(_diskCacheDir))
            {
                Directory.CreateDirectory(_diskCacheDir);
            }
        }
        catch
        {
            // Ignore disk cache folder creation errors
        }
    }

    /// <summary>
    /// Loads the brand logo. First checks remote URL cache/fetch from backend.
    /// If brandLogoUrl is missing or fails, falls back to local bundled brand assets using brandName.
    /// </summary>
    public static Bitmap GetBrandLogo(string? brandLogoUrl, string? brandName = null, Action<Bitmap>? onAsyncLoaded = null)
    {
        // 1. Try URL if provided
        if (!string.IsNullOrWhiteSpace(brandLogoUrl) && Uri.TryCreate(brandLogoUrl, UriKind.Absolute, out var uri))
        {
            string cacheKey = ComputeHash(brandLogoUrl);

            // In-memory cache
            if (_memoryCache.TryGetValue(cacheKey, out var cachedBitmap))
            {
                return cachedBitmap;
            }

            // Persistent disk cache
            string diskPath = Path.Combine(_diskCacheDir, $"{cacheKey}.png");
            if (File.Exists(diskPath))
            {
                try
                {
                    using var stream = File.OpenRead(diskPath);
                    var bitmap = new Bitmap(stream);
                    _memoryCache[cacheKey] = bitmap;
                    return bitmap;
                }
                catch
                {
                    // Fallback to re-download if file corrupted
                }
            }

            // Asynchronously download from backend API URL and cache to disk
            _ = Task.Run(async () =>
            {
                try
                {
                    using var response = await _httpClient.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] data = await response.Content.ReadAsByteArrayAsync();
                        if (data != null && data.Length > 0)
                        {
                            try
                            {
                                await File.WriteAllBytesAsync(diskPath, data);
                            }
                            catch { }

                            using var ms = new MemoryStream(data);
                            var bitmap = new Bitmap(ms);
                            _memoryCache[cacheKey] = bitmap;

                            if (onAsyncLoaded != null)
                            {
                                Dispatcher.UIThread.Post(() => onAsyncLoaded(bitmap));
                            }
                        }
                    }
                }
                catch { }
            });
        }

        // 2. Fallback to bundled brand assets if brandName is available
        if (!string.IsNullOrWhiteSpace(brandName))
        {
            var bundled = TryGetBundledBrandLogo(brandName);
            if (bundled != null)
            {
                return bundled;
            }
        }

        return DefaultLogo;
    }

    /// <summary>
    /// Attempts to load a bundled brand icon from avares://ADIapp/Assets/brands/{normalized}.png
    /// </summary>
    public static Bitmap? TryGetBundledBrandLogo(string brandName)
    {
        string normalized = NormalizeBrandName(brandName);
        if (string.IsNullOrEmpty(normalized)) return null;

        if (_memoryCache.TryGetValue("bundled_" + normalized, out var cached))
        {
            return cached;
        }

        try
        {
            var uri = new Uri($"avares://ADIapp/Assets/brands/{normalized}.png");
            if (AssetLoader.Exists(uri))
            {
                var bitmap = new Bitmap(AssetLoader.Open(uri));
                _memoryCache["bundled_" + normalized] = bitmap;
                return bitmap;
            }
        }
        catch { }

        return null;
    }

    public static string NormalizeBrandName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        string clean = name.Trim().ToLowerInvariant();

        if (clean == "vw" || clean.Contains("volkswagen")) return "volkswagen";
        if (clean.Contains("mercedes")) return "mercedes";
        if (clean.Contains("alfa")) return "alfa-romeo";
        if (clean.Contains("land") && clean.Contains("rover")) return "land-rover";

        clean = Regex.Replace(clean, @"\s+", "-");
        clean = Regex.Replace(clean, @"[^a-z0-9\-]", "");

        return clean;
    }

    private static string ComputeHash(string input)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
