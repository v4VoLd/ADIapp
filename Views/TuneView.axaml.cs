using Avalonia;
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
    private Border? _activeBorder;

    public TuneView()
    {
        InitializeComponent();
        SavedText = this.FindControl<TextBlock>("SavedText");
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        WebSocketManager.EcuIdentified += OnEcuIdentified;
        await LoadProcessingFilesAsync();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        WebSocketManager.EcuIdentified -= OnEcuIdentified;
    }

    private void OnEcuIdentified(string hash, EcuIdentifyData data)
    {
        // Refresh the processing tasks sidebar
        _ = LoadProcessingFilesAsync();

        if (_pendingFileHash == hash)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PopulateEcuInfo(data);
                RenderDynamicServices(data.Services);
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
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

    private async Task LoadProcessingFilesAsync()
    {
        try
        {
            var processingFiles = await ApiService.GetProcessingFilesAsync();

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateActiveFilesSidebar(processingFiles);
            });

            // If there's an active pending file hash we are currently waiting for, check if it finished
            if (!string.IsNullOrEmpty(_pendingFileHash))
            {
                bool isStillPending = processingFiles != null && processingFiles.Exists(f => f.FileHash == _pendingFileHash);
                if (!isStillPending)
                {
                    // It finished! Check its status and load the services
                    var response = await ApiService.CheckStatusAsync(_pendingFileHash);
                    if (response.Success && response.Data != null && response.Status == "completed")
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            PopulateEcuInfo(response.Data);
                            RenderDynamicServices(response.Data.Services);
                            if (StatusText != null) StatusText.Text = "Identified";
                            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                            if (ServicesContainer != null) ServicesContainer.IsVisible = true;
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading processing files: {ex.Message}", ex);
        }
    }

    private void UpdateActiveFilesSidebar(List<ProcessingFileDto> files)
    {
        var sidebar = this.FindControl<StackPanel>("ActiveFilesList");
        if (sidebar == null) return;

        sidebar.Children.Clear();

        if (files == null || files.Count == 0)
        {
            sidebar.Children.Add(new TextBlock
            {
                Text = "No active tasks",
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
            _activeBorder = null;
            return;
        }

        foreach (var file in files)
        {
            bool isActive = _pendingFileHash == file.FileHash;
            var itemBorder = new Border
            {
                Background = Avalonia.Media.Brush.Parse(isActive ? "#353535" : "#252525"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            if (isActive)
            {
                _activeBorder = itemBorder;
            }

            var stack = new StackPanel { Spacing = 4 };
            string truncatedHash = file.FileHash.Length > 12 ? file.FileHash.Substring(0, 12) + "..." : file.FileHash;

            stack.Children.Add(new TextBlock
            {
                Text = $"File: {truncatedHash}",
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.SemiBold
            });

            bool isCompleted = file.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);
            stack.Children.Add(new TextBlock
            {
                Text = isCompleted ? "IDENTIFIED" : "PROCESSING",
                Foreground = Avalonia.Media.Brush.Parse(isCompleted ? "#4DFF8A" : "#FFA500"),
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.Bold
            });

            itemBorder.Child = stack;

            string fileHash = file.FileHash;
            itemBorder.PointerPressed += async (s, e) =>
            {
                if (_activeBorder != null)
                {
                    _activeBorder.Background = Avalonia.Media.Brush.Parse("#252525");
                }
                itemBorder.Background = Avalonia.Media.Brush.Parse("#353535");
                _activeBorder = itemBorder;

                await SelectActiveFileAsync(fileHash);
            };

            sidebar.Children.Add(itemBorder);
        }
    }

    private async Task SelectActiveFileAsync(string hash)
    {
        _pendingFileHash = hash;

        if (StatusText != null) StatusText.Text = "Checking status...";
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        if (EcuBrandText != null) EcuBrandText.Text = "Loading...";
        if (EcuModelText != null) EcuModelText.Text = "Loading...";
        if (EcuHardwareText != null) EcuHardwareText.Text = "Loading...";
        if (EcuSoftwareText != null) EcuSoftwareText.Text = "Loading...";

        var response = await ApiService.CheckStatusAsync(hash);
        if (response.Success)
        {
            if (response.Status == "completed" && response.Data != null)
            {
                PopulateEcuInfo(response.Data);
                RenderDynamicServices(response.Data.Services);
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            }
            else
            {
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
            if (StatusText != null) StatusText.Text = "Failed";
            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF4D4D");
            if (EcuBrandText != null) EcuBrandText.Text = response.Message ?? "Error";
            if (ServicesContainer != null) ServicesContainer.IsVisible = false;
        }
    }

    private void RenderDynamicServices(List<ServiceDto>? services)
    {
        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel == null) return;

        panel.Children.Clear();

        if (services == null || services.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "No services available for this ECU.",
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 13,
                Margin = new Thickness(0, 10)
            });
            return;
        }

        foreach (var service in services)
        {
            var border = new Border
            {
                Background = Avalonia.Media.Brush.Parse("#252525"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(6),
                Width = 220
            };

            var stack = new StackPanel { Spacing = 6 };

            stack.Children.Add(new TextBlock
            {
                Text = service.Name,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"{service.Price} CBT Tokens",
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 11
            });

            var toggle = new ToggleSwitch
            {
                Tag = service.Id,
                OnContent = "Selected",
                OffContent = "Select",
                Margin = new Thickness(0, 4, 0, 0)
            };

            stack.Children.Add(toggle);
            border.Child = stack;
            panel.Children.Add(border);
        }
    }

    private async void OriginalFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isProcessing)
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
            _pendingFileHash = response.Data.FileHash;
            await LoadProcessingFilesAsync();

            if (response.Status == "completed")
            {
                PopulateEcuInfo(response.Data);
                RenderDynamicServices(response.Data.Services);
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            }
            else
            {
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
        if (string.IsNullOrEmpty(_pendingFileHash))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var window = topLevel as Window;
            if (window != null)
            {
                await MessageBox(window, "Please upload or select an identified file first.");
            }
            return;
        }

        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel == null) return;

        var selectedServiceIds = new List<int>();
        foreach (var child in panel.Children)
        {
            if (child is Border border && border.Child is StackPanel stack)
            {
                foreach (var innerChild in stack.Children)
                {
                    if (innerChild is ToggleSwitch toggle && toggle.IsChecked == true && toggle.Tag is int serviceId)
                    {
                        selectedServiceIds.Add(serviceId);
                    }
                }
            }
        }

        if (selectedServiceIds.Count == 0)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var window = topLevel as Window;
            if (window != null)
            {
                await MessageBox(window, "Please select at least one service to order.");
            }
            return;
        }

        var saveButton = sender as Button;
        if (saveButton != null)
        {
            saveButton.IsEnabled = false;
            saveButton.Content = "Saving...";
        }

        try
        {
            var (success, message) = await ApiService.CreateOrderAsync(_pendingFileHash, selectedServiceIds);

            var topLevel = TopLevel.GetTopLevel(this);
            var window = topLevel as Window;
            if (window != null)
            {
                await MessageBox(window, message);
            }

            if (success)
            {
                ResetWorkspace();
                await LoadProcessingFilesAsync();
            }
        }
        finally
        {
            if (saveButton != null)
            {
                saveButton.IsEnabled = true;
                saveButton.Content = "Save";
            }
        }
    }

    private void NewFileButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ResetWorkspace();
    }

    private void ResetWorkspace()
    {
        _pendingFileHash = null;
        if (_activeBorder != null)
        {
            _activeBorder.Background = Avalonia.Media.Brush.Parse("#252525");
            _activeBorder = null;
        }

        if (StatusText != null) StatusText.Text = "Ready";
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brushes.Gray;
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        if (EcuBrandText != null) EcuBrandText.Text = "Not Loaded";
        if (EcuModelText != null) EcuModelText.Text = "Not Loaded";
        if (EcuHardwareText != null) EcuHardwareText.Text = "Not Loaded";
        if (EcuSoftwareText != null) EcuSoftwareText.Text = "Not Loaded";

        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel != null) panel.Children.Clear();
    }

    private async Task MessageBox(Window window, string message)
    {
        var dialog = new Window
        {
            Width = 280,
            Height = 120,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(20),
                Background = Avalonia.Media.Brushes.Black,
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Avalonia.Media.Brushes.White,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            }
        };

        await dialog.ShowDialog(window);
    }
}
