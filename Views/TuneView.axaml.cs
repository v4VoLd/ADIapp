using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADIapp.Services;
using ADIapp.Models;
using ADIapp.Helpers;
using ADIapp.Config;

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
                RenderDynamicServices(data.GetEffectiveServices());
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            });
        }
    }

    private void PopulateEcuInfo(EcuIdentifyData data)
    {
        var vehTitle = this.FindControl<TextBlock>("VehicleTitleText");
        var vehSub = this.FindControl<TextBlock>("VehicleSubtitleText");

        var vehProducer = this.FindControl<TextBlock>("VehProducerText");
        var vehModel = this.FindControl<TextBlock>("VehModelText");
        var vehYearChassis = this.FindControl<TextBlock>("VehYearChassisText");
        var vehBuildType = this.FindControl<TextBlock>("VehBuildTypeText");
        var vehVin = this.FindControl<TextBlock>("VehVinText");

        var engNameType = this.FindControl<TextBlock>("EngNameTypeText");
        var engDisplacement = this.FindControl<TextBlock>("EngDisplacementText");
        var engOutput = this.FindControl<TextBlock>("EngOutputText");
        var engEmission = this.FindControl<TextBlock>("EngEmissionText");
        var engTransmission = this.FindControl<TextBlock>("EngTransmissionText");

        var ecuBrand = this.FindControl<TextBlock>("EcuBrandText");
        var ecuHardware = this.FindControl<TextBlock>("EcuHardwareText");
        var ecuProdNr = this.FindControl<TextBlock>("EcuProdNrText");
        var ecuSoftware = this.FindControl<TextBlock>("EcuSoftwareText");
        var ecuSize = this.FindControl<TextBlock>("EcuSoftwareSizeText");

        if (vehTitle != null)
        {
            vehTitle.Text = !string.IsNullOrWhiteSpace(data.FullVehicleTitle) 
                ? data.FullVehicleTitle 
                : "Vehicle & ECU Information";
        }

        if (vehSub != null)
        {
            var subtitleParts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(data.VehicleVIN)) subtitleParts.Add($"VIN: {data.VehicleVIN}");
            if (!string.IsNullOrWhiteSpace(data.VehicleModelyear)) subtitleParts.Add($"Year: {data.VehicleModelyear}");
            if (!string.IsNullOrWhiteSpace(data.EngineTransmission)) subtitleParts.Add($"Trans: {data.EngineTransmission}");
            vehSub.Text = subtitleParts.Count > 0 
                ? string.Join(" | ", subtitleParts) 
                : "ECU identification completed successfully.";
        }

        if (vehProducer != null) vehProducer.Text = !string.IsNullOrWhiteSpace(data.VehicleProducer) ? data.VehicleProducer : "N/A";
        if (vehModel != null) vehModel.Text = !string.IsNullOrWhiteSpace(data.VehicleModel) ? data.VehicleModel : "N/A";

        if (vehYearChassis != null)
        {
            string year = !string.IsNullOrWhiteSpace(data.VehicleModelyear) ? data.VehicleModelyear : "-";
            string chassis = !string.IsNullOrWhiteSpace(data.VehicleChassis) ? data.VehicleChassis : "-";
            vehYearChassis.Text = $"{year} / {chassis}";
        }

        if (vehBuildType != null)
        {
            string build = !string.IsNullOrWhiteSpace(data.VehicleBuild) ? data.VehicleBuild : "-";
            string type = !string.IsNullOrWhiteSpace(data.VehicleType) ? data.VehicleType : "-";
            vehBuildType.Text = $"{build} / {type}";
        }

        if (vehVin != null) vehVin.Text = !string.IsNullOrWhiteSpace(data.VehicleVIN) ? data.VehicleVIN : "N/A";

        if (engNameType != null)
        {
            string name = !string.IsNullOrWhiteSpace(data.EngineName) ? data.EngineName : "-";
            string type = !string.IsNullOrWhiteSpace(data.EngineType) ? data.EngineType : "-";
            engNameType.Text = $"{name} ({type})";
        }

        if (engDisplacement != null)
        {
            engDisplacement.Text = !string.IsNullOrWhiteSpace(data.EngineDisplacement) ? $"{data.EngineDisplacement}L" : "N/A";
        }

        if (engOutput != null)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(data.EngineOutputPS)) parts.Add($"{data.EngineOutputPS} PS");
            if (!string.IsNullOrWhiteSpace(data.EngineOutputKW)) parts.Add($"{data.EngineOutputKW} kW");
            engOutput.Text = parts.Count > 0 ? string.Join(" / ", parts) : "N/A";
        }

        if (engEmission != null) engEmission.Text = !string.IsNullOrWhiteSpace(data.EngineEmissionStd) ? data.EngineEmissionStd : "N/A";
        if (engTransmission != null) engTransmission.Text = !string.IsNullOrWhiteSpace(data.EngineTransmission) ? data.EngineTransmission : "N/A";

        if (ecuBrand != null)
        {
            string producer = !string.IsNullOrWhiteSpace(data.EcuProducer) ? data.EcuProducer : data.EcuBrand;
            string build = !string.IsNullOrWhiteSpace(data.EcuBuild) ? data.EcuBuild : data.EcuModel;
            ecuBrand.Text = $"{producer} {build}".Trim();
        }

        if (ecuHardware != null)
        {
            ecuHardware.Text = !string.IsNullOrWhiteSpace(data.EcuStgNr) ? data.EcuStgNr : data.HardwareId;
        }

        if (ecuProdNr != null)
        {
            ecuProdNr.Text = !string.IsNullOrWhiteSpace(data.EcuProdNr) ? data.EcuProdNr : "N/A";
        }

        if (ecuSoftware != null)
        {
            ecuSoftware.Text = !string.IsNullOrWhiteSpace(data.EcuSoftwareVersion) ? data.EcuSoftwareVersion : data.SoftwareId;
        }

        if (ecuSize != null)
        {
            ecuSize.Text = !string.IsNullOrWhiteSpace(data.EcuSoftwareSize) ? $"{data.EcuSoftwareSize} bytes" : "N/A";
        }
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
                            RenderDynamicServices(response.Data.GetEffectiveServices());
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

            if (file.IsOrder)
            {
                // Active Order Item Rendering
                string displayTitle = !string.IsNullOrEmpty(file.Title) ? file.Title : $"Order #{file.OrderId}";
                stack.Children.Add(new TextBlock
                {
                    Text = $"⚡ {displayTitle}",
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 13,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                });

                string statusText = file.Status.ToUpper();
                string statusColor = file.Status.Equals("finished", StringComparison.OrdinalIgnoreCase)
                    ? "#4DFF8A"
                    : (file.Status.Equals("processing", StringComparison.OrdinalIgnoreCase) ? "#2196F3" : "#FFA500");

                var statusPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
                statusPanel.Children.Add(new TextBlock
                {
                    Text = $"ORDER • {statusText}",
                    Foreground = Avalonia.Media.Brush.Parse(statusColor),
                    FontSize = 10,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                stack.Children.Add(statusPanel);

                if (file.Status.Equals("finished", StringComparison.OrdinalIgnoreCase))
                {
                    string targetDownloadUrl = file.DownloadUrl ?? $"{AppConfig.BaseUrl}/order/download/{file.OrderId}";
                    string fileName = file.FileSent ?? $"order_{file.OrderId}_mod.bin";

                    var downloadBtn = new Button
                    {
                        Content = "⬇ Download Mod File",
                        Background = Avalonia.Media.Brush.Parse("#4DFF8A"),
                        Foreground = Avalonia.Media.Brushes.Black,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        FontSize = 11,
                        Padding = new Thickness(8, 4),
                        Margin = new Thickness(0, 4, 0, 0),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
                    };

                    downloadBtn.Click += async (s, e) =>
                    {
                        downloadBtn.IsEnabled = false;
                        downloadBtn.Content = "Downloading...";

                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel is Window window)
                        {
                            var saveFile = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                            {
                                Title = "Save Modified Tuning File",
                                SuggestedFileName = fileName
                            });

                            if (saveFile != null)
                            {
                                using var stream = await saveFile.OpenWriteAsync();
                                var (success, msg) = await ApiService.DownloadFileToStreamAsync(targetDownloadUrl, stream);
                                if (success)
                                {
                                    downloadBtn.Content = "✓ Downloaded";
                                }
                                else
                                {
                                    downloadBtn.IsEnabled = true;
                                    downloadBtn.Content = "Retry Download";
                                    await MessageBox(window, $"Download failed: {msg}");
                                }
                            }
                            else
                            {
                                downloadBtn.IsEnabled = true;
                                downloadBtn.Content = "⬇ Download Mod File";
                            }
                        }
                    };

                    stack.Children.Add(downloadBtn);
                }
            }
            else
            {
                // Identified Map Item Rendering
                string displayTitle = !string.IsNullOrEmpty(file.Title) ? file.Title : $"Map: {truncatedHash}";
                stack.Children.Add(new TextBlock
                {
                    Text = $"🔍 {displayTitle}",
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 13,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                });

                bool isCompleted = file.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);
                stack.Children.Add(new TextBlock
                {
                    Text = isCompleted ? "MAP IDENTIFIED" : "IDENTIFICATION PENDING",
                    Foreground = Avalonia.Media.Brush.Parse(isCompleted ? "#4DFF8A" : "#FFA500"),
                    FontSize = 10,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });
            }

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

    private void SetCardsPendingState(string stateText = "Pending...")
    {
        var vehTitle = this.FindControl<TextBlock>("VehicleTitleText");
        var vehSub = this.FindControl<TextBlock>("VehicleSubtitleText");

        var vehProducer = this.FindControl<TextBlock>("VehProducerText");
        var vehModel = this.FindControl<TextBlock>("VehModelText");
        var vehYearChassis = this.FindControl<TextBlock>("VehYearChassisText");
        var vehBuildType = this.FindControl<TextBlock>("VehBuildTypeText");
        var vehVin = this.FindControl<TextBlock>("VehVinText");

        var engNameType = this.FindControl<TextBlock>("EngNameTypeText");
        var engDisplacement = this.FindControl<TextBlock>("EngDisplacementText");
        var engOutput = this.FindControl<TextBlock>("EngOutputText");
        var engEmission = this.FindControl<TextBlock>("EngEmissionText");
        var engTransmission = this.FindControl<TextBlock>("EngTransmissionText");

        var ecuBrand = this.FindControl<TextBlock>("EcuBrandText");
        var ecuHardware = this.FindControl<TextBlock>("EcuHardwareText");
        var ecuProdNr = this.FindControl<TextBlock>("EcuProdNrText");
        var ecuSoftware = this.FindControl<TextBlock>("EcuSoftwareText");
        var ecuSize = this.FindControl<TextBlock>("EcuSoftwareSizeText");

        if (vehTitle != null) vehTitle.Text = "Vehicle & ECU Information";
        if (vehSub != null)
        {
            vehSub.Text = stateText.Equals("Not Loaded", StringComparison.OrdinalIgnoreCase) || stateText.Equals("-", StringComparison.OrdinalIgnoreCase)
                ? "Upload a binary file or select an active task to inspect specifications."
                : $"Status: {stateText}";
        }

        if (vehProducer != null) vehProducer.Text = stateText;
        if (vehModel != null) vehModel.Text = stateText;
        if (vehYearChassis != null) vehYearChassis.Text = stateText;
        if (vehBuildType != null) vehBuildType.Text = stateText;
        if (vehVin != null) vehVin.Text = stateText;

        if (engNameType != null) engNameType.Text = stateText;
        if (engDisplacement != null) engDisplacement.Text = stateText;
        if (engOutput != null) engOutput.Text = stateText;
        if (engEmission != null) engEmission.Text = stateText;
        if (engTransmission != null) engTransmission.Text = stateText;

        if (ecuBrand != null) ecuBrand.Text = stateText;
        if (ecuHardware != null) ecuHardware.Text = stateText;
        if (ecuProdNr != null) ecuProdNr.Text = stateText;
        if (ecuSoftware != null) ecuSoftware.Text = stateText;
        if (ecuSize != null) ecuSize.Text = stateText;
    }

    private async Task SelectActiveFileAsync(string hash)
    {
        _pendingFileHash = hash;

        SetCardsPendingState("Checking status...");

        if (StatusText != null) StatusText.Text = "Checking status...";
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        var response = await ApiService.CheckStatusAsync(hash);
        if (response.Success)
        {
            if (response.Status == "completed" && response.Data != null)
            {
                PopulateEcuInfo(response.Data);
                var effectiveServices = response.Data.GetEffectiveServices();

                bool isFailed = string.Equals(response.Data.Status, "failed", StringComparison.OrdinalIgnoreCase);

                if (isFailed || !response.Data.IsSupported || effectiveServices == null || effectiveServices.Count == 0)
                {
                    if (StatusText != null) StatusText.Text = "Unsupported ECU";
                    if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF9800");
                    RenderUnsupportedEcuUi(response.Data);
                }
                else
                {
                    if (StatusText != null) StatusText.Text = "Identified";
                    if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                    RenderDynamicServices(effectiveServices);
                }

                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            }
            else
            {
                SetCardsPendingState("Pending...");

                if (StatusText != null) StatusText.Text = "Processing...";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
                if (ServicesContainer != null) ServicesContainer.IsVisible = false;
            }
        }
        else
        {
            if (StatusText != null) StatusText.Text = "Failed";
            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF4D4D");

            if (response.Data != null)
            {
                PopulateEcuInfo(response.Data);
                RenderUnsupportedEcuUi(response.Data);
            }
            else
            {
                SetCardsPendingState("Failed");
            }
            if (ServicesContainer != null) ServicesContainer.IsVisible = true;
        }
    }

    private void RenderUnsupportedEcuUi(EcuIdentifyData data)
    {
        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel == null) return;

        panel.Children.Clear();

        var border = new Border
        {
            Background = Avalonia.Media.Brush.Parse("#2C1D11"),
            BorderBrush = Avalonia.Media.Brush.Parse("#FF9800"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 10),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        var stack = new StackPanel { Spacing = 12 };

        stack.Children.Add(new TextBlock
        {
            Text = "⚠️ ECU File Not Found in Database",
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brush.Parse("#FFB74D")
        });

        stack.Children.Add(new TextBlock
        {
            Text = "This ECU reference is not mapped to services yet. Submit a support ticket so our technical team can add it for you.",
            FontSize = 12,
            Foreground = Avalonia.Media.Brush.Parse("#CCCCCC"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var submitBtn = new Button
        {
            Content = "📩 Submit Support Ticket for this ECU",
            Background = Avalonia.Media.Brush.Parse("#FF9800"),
            Foreground = Avalonia.Media.Brushes.Black,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Padding = new Thickness(16, 8),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 0)
        };

        submitBtn.Click += async (s, e) =>
        {
            submitBtn.IsEnabled = false;
            submitBtn.Content = "Sending Ticket...";

            string ticketContent = $"[ECU Support Request]\n" +
                                   $"File Hash: {data.FileHash}\n" +
                                   $"ECU Brand: {data.EcuBrand}\n" +
                                   $"ECU Model: {data.EcuModel}\n" +
                                   $"Hardware ID: {data.HardwareId}\n" +
                                   $"Software ID: {data.SoftwareId}";

            var res = await ApiService.SendSupportMessageAsync(ticketContent);
            if (res.Success)
            {
                submitBtn.Content = "✓ Support Ticket Sent";
                submitBtn.Background = Avalonia.Media.Brush.Parse("#4CAF50");
                submitBtn.Foreground = Avalonia.Media.Brushes.White;
            }
            else
            {
                submitBtn.IsEnabled = true;
                submitBtn.Content = "Retry Sending Ticket";
            }
        };

        stack.Children.Add(submitBtn);
        border.Child = stack;
        panel.Children.Add(border);
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
        SetCardsPendingState("Uploading...");

        if (StatusText != null) StatusText.Text = "Uploading...";
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        var response = await ApiService.UploadAndIdentifyEcuAsync(filePath);
        _isProcessing = false;

        if (response.Success && response.Data != null)
        {
            _pendingFileHash = response.Data.FileHash;
            await LoadProcessingFilesAsync();

            if (response.Status == "completed")
            {
                PopulateEcuInfo(response.Data);
                RenderDynamicServices(response.Data.GetEffectiveServices());
                if (StatusText != null) StatusText.Text = "Identified";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            }
            else
            {
                SetCardsPendingState("Pending...");

                if (StatusText != null) StatusText.Text = "Processing...";
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
                if (ServicesContainer != null) ServicesContainer.IsVisible = false;
            }
        }
        else
        {
            _pendingFileHash = null;
            if (StatusText != null) StatusText.Text = "Failed";
            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF4D4D");
            SetCardsPendingState("Failed");
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
        var selectedServiceNames = new List<string>();

        foreach (var child in panel.Children)
        {
            if (child is Border border && border.Child is StackPanel stack)
            {
                foreach (var innerChild in stack.Children)
                {
                    if (innerChild is ToggleSwitch toggle && toggle.IsChecked == true)
                    {
                        if (toggle.Tag is ServiceDto service)
                        {
                            selectedServiceIds.Add(service.Id);
                            if (!string.IsNullOrEmpty(service.Name)) selectedServiceNames.Add(service.Name);
                        }
                        else if (toggle.Tag is int serviceId)
                        {
                            selectedServiceIds.Add(serviceId);
                        }
                    }
                }
            }
        }

        if (selectedServiceIds.Count == 0 && selectedServiceNames.Count == 0)
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
            var (success, message) = await ApiService.CreateOrderAsync(_pendingFileHash, selectedServiceIds, selectedServiceNames);

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

        SetCardsPendingState("Not Loaded");

        if (StatusText != null) StatusText.Text = "Ready";
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brushes.Gray;
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

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
