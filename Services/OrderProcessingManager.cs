using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ADIapp.Models;
using ADIapp.Config;

namespace ADIapp.Services;

public static class OrderProcessingManager
{
    private static string? _activeOrderFileHash;
    private static bool _isProcessing;
    private static string _statusText = string.Empty;
    private static bool _isDownloading;

    public static string? ActiveOrderFileHash => _activeOrderFileHash;
    public static bool IsProcessing => _isProcessing;
    public static string StatusText => _statusText;

    public static event Action? StateChanged;

    public static void Initialize()
    {
        WebSocketManager.OrderUpdated -= OnWebSocketOrderUpdated;
        WebSocketManager.OrderUpdated += OnWebSocketOrderUpdated;
    }

    public static void StartTrackingOrder(string fileHash)
    {
        _activeOrderFileHash = fileHash;
        _isProcessing = true;
        _statusText = LanguageService.Get("Tune_WaitingTuning");
        StateChanged?.Invoke();

        // 1. Initial instant check in case backend finished during request
        _ = CheckAndHandleOrderCompletionAsync(fileHash);

        // 2. Slow safety fallback check in case websocket packet dropped
        _ = FallbackSafetyCheckAsync(fileHash);
    }

    public static void StopTracking()
    {
        _activeOrderFileHash = null;
        _isProcessing = false;
        _statusText = string.Empty;
        StateChanged?.Invoke();
    }

    private static void OnWebSocketOrderUpdated()
    {
        if (string.IsNullOrEmpty(_activeOrderFileHash) || !_isProcessing)
            return;

        // WebSocket event received: trigger instant status resolution (0ms delay)
        Dispatcher.UIThread.Post(async () =>
        {
            await CheckAndHandleOrderCompletionAsync(_activeOrderFileHash);
        });
    }

    private static async Task FallbackSafetyCheckAsync(string fileHash)
    {
        // Check only 4 times at 15s intervals (up to 60s total) purely as a safety net
        for (int i = 0; i < 4; i++)
        {
            await Task.Delay(15000);

            if (_activeOrderFileHash != fileHash || !_isProcessing)
                return;

            bool handled = await CheckAndHandleOrderCompletionAsync(fileHash);
            if (handled)
                return;
        }

        // Timeout fallback
        if (_activeOrderFileHash == fileHash)
        {
            _isProcessing = false;
            _statusText = string.Empty;
            StateChanged?.Invoke();
        }
    }

    private static async Task<bool> CheckAndHandleOrderCompletionAsync(string fileHash)
    {
        if (_isDownloading) return false;

        try
        {
            var history = await ApiService.GetOrderHistoryAsync();
            if (history?.Orders != null)
            {
                var matchingOrder = history.Orders.FirstOrDefault(o =>
                    (!string.IsNullOrEmpty(o.FileReceived) && o.FileReceived.Contains(fileHash, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.FileSent) && o.FileSent.Contains(fileHash, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.DownloadUrl) && o.DownloadUrl.Contains(fileHash, StringComparison.OrdinalIgnoreCase))
                );

                if (matchingOrder == null && history.Orders.Count > 0)
                {
                    matchingOrder = history.Orders[0];
                }

                if (matchingOrder != null)
                {
                    if (matchingOrder.IsCompleted)
                    {
                        _isProcessing = false;
                        _activeOrderFileHash = null;
                        _statusText = string.Empty;
                        StateChanged?.Invoke();

                        await PromptAndDownloadFileAsync(matchingOrder);
                        return true;
                    }
                    else if (matchingOrder.IsCanceled)
                    {
                        _isProcessing = false;
                        _activeOrderFileHash = null;
                        _statusText = string.Empty;
                        StateChanged?.Invoke();
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Helpers.Logger.Error($"Error checking order completion: {ex.Message}", ex);
        }

        return false;
    }

    public static async Task PromptAndDownloadFileAsync(OrderHistoryItemDto order)
    {
        if (_isDownloading) return;
        _isDownloading = true;

        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var window = lifetime?.MainWindow;
            if (window == null) return;

            string downloadUrl = order.DownloadUrl ?? $"{AppConfig.BaseUrl}/order/download/{order.Id}";
            string fileName = order.FileSent ?? $"order_{order.Id}_mod.bin";

            var saveFile = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Modified Tuning File",
                SuggestedFileName = fileName
            });

            if (saveFile != null)
            {
                using var stream = await saveFile.OpenWriteAsync();
                var progress = new System.Progress<double>(p => { });

                var (success, msg) = await ApiService.DownloadFileToStreamAsync(downloadUrl, stream, progress);
                if (success)
                {
                    NotificationService.AddNotification(
                        $"order_download_{order.Id}",
                        string.Format(LanguageService.Get("Tune_SavedSuccess"), fileName),
                        "info"
                    );
                    _ = ApiService.FetchProfileAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Helpers.Logger.Error($"Error saving file: {ex.Message}", ex);
        }
        finally
        {
            _isDownloading = false;
        }
    }
}
