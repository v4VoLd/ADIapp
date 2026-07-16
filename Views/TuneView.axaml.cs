using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADIapp.Services;
using ADIapp.Models;
using ADIapp.Helpers;

namespace ADIapp.Views;

public partial class TuneView : UserControl
{
    private TextBlock? SavedText;
    private string? _pendingFileHash;
    private bool _isProcessing;

    public TuneView()
    {
        InitializeComponent();
        SavedText = this.FindControl<TextBlock>("SavedText");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        WebSocketManager.EcuIdentified += OnEcuIdentified;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        WebSocketManager.EcuIdentified -= OnEcuIdentified;
    }

    private void OnEcuIdentified(string hash, EcuIdentifyData data)
    {
        if (_pendingFileHash == hash)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PopulateEcuInfo(data);
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
                _pendingFileHash = null;
            });
        }
    }

    private void PopulateEcuInfo(EcuIdentifyData data)
    {
        if (EcuBrandText != null) EcuBrandText.Text = data.EcuBrand;
        if (EcuModelText != null) EcuModelText.Text = data.EcuModel;
        if (EcuHardwareText != null) EcuHardwareText.Text = data.HardwareId;
        if (EcuSoftwareText != null) EcuSoftwareText.Text = data.SoftwareId;
    }

    private async void OriginalFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isProcessing || !string.IsNullOrEmpty(_pendingFileHash))
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select ECU Binary File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Binary Files") { Patterns = new[] { "*.bin", "*.hex", "*.ori", "*.dec" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count >= 1)
        {
            var file = files[0];
            string filePath = file.Path.LocalPath;
            await ProcessAndUploadFileAsync(filePath);
        }
    }

    private async Task ProcessAndUploadFileAsync(string filePath)
    {
        _isProcessing = true;
        if (StatusText != null) StatusText.Text = "Uploading...";
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        if (EcuBrandText != null) EcuBrandText.Text = "...";
        if (EcuModelText != null) EcuModelText.Text = "...";
        if (EcuHardwareText != null) EcuHardwareText.Text = "...";
        if (EcuSoftwareText != null) EcuSoftwareText.Text = "...";

        var response = await ApiService.UploadAndIdentifyEcuAsync(filePath);
        _isProcessing = false;

        if (response.Success && response.Data != null)
        {
            if (response.Status == "completed")
            {
                PopulateEcuInfo(response.Data);
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
                _pendingFileHash = null;
            }
            else
            {
                // Status is pending (queued)
                _pendingFileHash = response.Data.FileHash;
                if (StatusText != null) StatusText.Text = "Processing...";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
                if (ServicesContainer != null) ServicesContainer.IsVisible = false;

                if (EcuBrandText != null) EcuBrandText.Text = "Queued";
                if (EcuModelText != null) EcuModelText.Text = "Queued";
                if (EcuHardwareText != null) EcuHardwareText.Text = "Queued";
                if (EcuSoftwareText != null) EcuSoftwareText.Text = "Queued";
            }
        }
        else
        {
            _pendingFileHash = null;
            if (StatusText != null) StatusText.Text = "Failed";
            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF4D4D");
            if (EcuBrandText != null) EcuBrandText.Text = response.Message ?? "Error";
            if (ServicesContainer != null) ServicesContainer.IsVisible = false;
        }
    }

    private async void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SavedText != null)
        {
            SavedText.IsVisible = true;
            await Task.Delay(1500);
            SavedText.IsVisible = false;
        }
    }
}
