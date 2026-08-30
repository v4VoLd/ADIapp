using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private string? _renderedFileHash;
    private bool _isProcessing;
    private bool _isIdentifying;
    private Border? _activeBorder;

    private List<ServiceDto>? _currentServices;
    private string _activeFilter = "ALL";
    private readonly Dictionary<int, bool> _serviceSelectionStates = new();

    private enum ServiceCategory
    {
        Performance,
        Deletes,
        Features
    }

    public TuneView()
    {
        InitializeComponent();
        SavedText = this.FindControl<TextBlock>("SavedText");
        AddHandler(DragDrop.DropEvent, OnFileDrop);
        AddHandler(DragDrop.DragOverEvent, OnFileDragOver);
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        WebSocketManager.EcuIdentified += OnEcuIdentified;
        WebSocketManager.OrderUpdated += OnOrderUpdated;
        ApiService.CurrentUserChanged += OnCurrentUserChanged;

        OrderProcessingManager.StateChanged += OnOrderProcessingStateChanged;

        LanguageService.LanguageChanged += OnLanguageChanged;
        UpdateLocalizedText();
        UpdateDailyQuotaUi();
        UpdateOrderProgressModal();
        await LoadProcessingFilesAsync();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        WebSocketManager.EcuIdentified -= OnEcuIdentified;
        WebSocketManager.OrderUpdated -= OnOrderUpdated;
        ApiService.CurrentUserChanged -= OnCurrentUserChanged;
        OrderProcessingManager.StateChanged -= OnOrderProcessingStateChanged;
        LanguageService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnCurrentUserChanged(Models.UserDto? user)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateDailyQuotaUi();
        });
    }

    private void OnOrderProcessingStateChanged()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateOrderProgressModal();
        });
    }

    private void UpdateOrderProgressModal()
    {
        if (OrderProgressOverlay != null)
        {
            OrderProgressOverlay.IsVisible = OrderProcessingManager.IsProcessing;
        }
        if (OrderProgressTitle != null)
        {
            OrderProgressTitle.Text = LanguageService.Get("Tune_ProcessingOrder");
        }
        if (OrderProgressStatus != null)
        {
            OrderProgressStatus.Text = !string.IsNullOrEmpty(OrderProcessingManager.StatusText)
                ? OrderProcessingManager.StatusText
                : LanguageService.Get("Tune_WaitingTuning");
        }
    }

    private void OnLanguageChanged()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateLocalizedText();
            UpdateDailyQuotaUi();
        });
    }

    private void OnOrderUpdated()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _ = LoadProcessingFilesAsync();
            _ = ApiService.FetchProfileAsync();
        });
    }

    private void UpdateDailyQuotaUi()
    {
        /* Quota UI disabled
        var quotaTitle = this.FindControl<TextBlock>("TuneDailyQuotaTitleText");
        var quotaValue = this.FindControl<TextBlock>("TuneDailyQuotaValueText");

        if (quotaTitle != null)
        {
            quotaTitle.Text = LanguageService.Get("Sidebar_DailyQuota");
        }

        if (quotaValue != null)
        {
            var user = ApiService.CurrentUser;
            if (user != null && user.HasDailyLimit)
            {
                int used = user.TodayOrdersCount ?? 0;
                int limit = user.OrderLimit!.Value;
                quotaValue.Text = $"{used} / {limit} files";
                quotaValue.Foreground = user.HasReachedDailyLimit
                    ? Avalonia.Media.Brush.Parse("#FF4D4D")
                    : Avalonia.Media.Brush.Parse("#4DFF8A");
            }
            else
            {
                quotaValue.Text = LanguageService.Get("Quota_Unlimited");
                quotaValue.Foreground = Avalonia.Media.Brush.Parse("#4DFF8A");
            }
        }
        */

        UpdateSummaryAndSaveButton();
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new HomeView());
    }

    private async void OriginalFile_Click(object? sender, RoutedEventArgs e)
    {
        await OpenFilePickerAndUploadAsync();
    }

    private void UpdateLocalizedText()
    {
        if (VehicleTitleText != null) VehicleTitleText.Text = LanguageService.Get("Tune_Title");
        if (VehicleSubtitleText != null) VehicleSubtitleText.Text = LanguageService.Get("Tune_Subtitle");
        if (BackButton != null) BackButton.Content = LanguageService.Get("Tune_Back");

        if (VehSpecsTitleText != null) VehSpecsTitleText.Text = LanguageService.Get("Tune_VehSpecs");
        if (LblProducerText != null) LblProducerText.Text = LanguageService.Get("Tune_Producer");
        if (LblModelText != null) LblModelText.Text = LanguageService.Get("Tune_Model");
        if (LblYearChassisText != null) LblYearChassisText.Text = LanguageService.Get("Tune_YearChassis");
        if (LblBuildTypeText != null) LblBuildTypeText.Text = LanguageService.Get("Tune_BuildType");

        if (EngSpecsTitleText != null) EngSpecsTitleText.Text = LanguageService.Get("Tune_EngSpecs");
        if (LblNameTypeText != null) LblNameTypeText.Text = LanguageService.Get("Tune_NameType");
        if (LblDisplacementText != null) LblDisplacementText.Text = LanguageService.Get("Tune_Displacement");
        if (LblOutputText != null) LblOutputText.Text = LanguageService.Get("Tune_Output");
        if (LblEmissionText != null) LblEmissionText.Text = LanguageService.Get("Tune_Emission");
        if (LblTransmissionText != null) LblTransmissionText.Text = LanguageService.Get("Tune_Transmission");

        if (EcuSpecsTitleText != null) EcuSpecsTitleText.Text = LanguageService.Get("Tune_EcuSpecs");
        if (LblProdBuildText != null) LblProdBuildText.Text = LanguageService.Get("Tune_ProdBuild");
        if (LblHwNrText != null) LblHwNrText.Text = LanguageService.Get("Tune_HwNr");
        if (LblProdNrText != null) LblProdNrText.Text = LanguageService.Get("Tune_ProdNr");
        if (LblSwVersionText != null) LblSwVersionText.Text = LanguageService.Get("Tune_SwVersion");
        if (LblSwSizeText != null) LblSwSizeText.Text = LanguageService.Get("Tune_SwSize");

        if (AvailableTunesTitleText != null) AvailableTunesTitleText.Text = LanguageService.Get("Tune_AvailableTunes");

        var filterAll = this.FindControl<Button>("FilterAllButton");
        var filterPerf = this.FindControl<Button>("FilterPerfButton");
        var filterDeletes = this.FindControl<Button>("FilterDeletesButton");
        var filterFeatures = this.FindControl<Button>("FilterFeaturesButton");

        if (filterAll != null) filterAll.Content = LanguageService.Get("Tune_FilterAll");
        if (filterPerf != null) filterPerf.Content = LanguageService.Get("Tune_FilterPerf");
        if (filterDeletes != null) filterDeletes.Content = LanguageService.Get("Tune_FilterDeletes");
        if (filterFeatures != null) filterFeatures.Content = LanguageService.Get("Tune_FilterFeatures");

        var dragDropHint = this.FindControl<TextBlock>("DragDropHintText");
        if (dragDropHint != null) dragDropHint.Text = LanguageService.Get("Tune_DragAndDrop");

        UpdateSummaryAndSaveButton();
    }

    private void OnEcuIdentified(string hash, EcuIdentifyData data)
    {
        // Refresh the processing tasks sidebar
        _ = LoadProcessingFilesAsync();

        if (_pendingFileHash == hash)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isIdentifying = false;
                PopulateEcuInfo(data);

                bool isFailed = string.Equals(data?.Status, "failed", StringComparison.OrdinalIgnoreCase) || data?.IsSupported == false;
                var effectiveServices = data?.GetEffectiveServices();

                if (isFailed || effectiveServices == null || effectiveServices.Count == 0)
                {
                    if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusUnsupported");
                    if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF9800");
                    RenderUnsupportedEcuUi(data ?? new EcuIdentifyData { FileHash = hash });
                }
                else
                {
                    RenderDynamicServices(effectiveServices);
                    if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusIdentified");
                    if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                }

                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
                _renderedFileHash = hash;
                UpdateSummaryAndSaveButton();
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
            if (!string.IsNullOrEmpty(_pendingFileHash) && _renderedFileHash != _pendingFileHash)
            {
                var currentItem = processingFiles?.Find(f => f.FileHash == _pendingFileHash);
                bool isStillPending = currentItem != null && currentItem.Status.Equals("pending", StringComparison.OrdinalIgnoreCase);

                if (!isStillPending)
                {
                    // It finished or failed! Check status and render appropriate UI
                    var response = await ApiService.CheckStatusAsync(_pendingFileHash);
                    if (response.Data != null)
                    {
                        var effectiveServices = response.Data.GetEffectiveServices();
                        bool isFailed = string.Equals(response.Status, "failed", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(response.Data.Status, "failed", StringComparison.OrdinalIgnoreCase)
                            || !response.Data.IsSupported;

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            PopulateEcuInfo(response.Data);

                            if (isFailed || effectiveServices == null || effectiveServices.Count == 0)
                            {
                                if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusUnsupported");
                                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF9800");
                                RenderUnsupportedEcuUi(response.Data);
                            }
                            else
                            {
                                RenderDynamicServices(effectiveServices);
                                if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusIdentified");
                                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                            }

                            if (ServicesContainer != null) ServicesContainer.IsVisible = true;
                            _renderedFileHash = _pendingFileHash;

                            NotificationService.AddNotification($"ecu_done_{_pendingFileHash}", LanguageService.Get("Tune_EcuIdentDone"), "info");
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
            if (file == null) continue;
            string fileHashStr = file.FileHash ?? string.Empty;
            bool isActive = !string.IsNullOrEmpty(_pendingFileHash) && _pendingFileHash == fileHashStr;
            var itemBorder = new Border
            {
                Background = Avalonia.Media.Brush.Parse(isActive ? "#353535" : "#252525"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            if (isActive)
            {
                _activeBorder = itemBorder;
            }

            var stack = new StackPanel { Spacing = 6 };
            string truncatedHash = fileHashStr.Length > 12 ? fileHashStr.Substring(0, 12) + "..." : fileHashStr;

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
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 2)
                });

                string statusText = file.Status.Replace("_", " ").ToUpper();
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
                        Content = LanguageService.Get("Download_ModFile"),
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
                        downloadBtn.Content = LanguageService.Get("Tune_Downloading");

                        var (success, msg) = await DownloadAndSaveFileAsync(
                            targetDownloadUrl,
                            fileName,
                            progressText => downloadBtn.Content = progressText
                        );

                        if (success)
                        {
                            downloadBtn.Content = LanguageService.Get("Tune_Downloaded");
                            NotificationService.AddNotification($"download_done_{fileName}", string.Format(LanguageService.Get("Tune_FileDownloadCompleted"), fileName), "info");
                        }
                        else
                        {
                            downloadBtn.IsEnabled = true;
                            downloadBtn.Content = LanguageService.Get("Download_ModFile");
                            if (!string.IsNullOrEmpty(msg))
                            {
                                var topLevel = TopLevel.GetTopLevel(this);
                                if (topLevel is Window window)
                                {
                                    await MessageBox(window, string.Format(LanguageService.Get("Tune_DownloadFailed"), msg));
                                }
                            }
                        }
                    };

                    stack.Children.Add(downloadBtn);
                }
            }
            else if (file.IsTicket)
            {
                // Pending Ticket Item Rendering
                string displayTitle = !string.IsNullOrEmpty(file.Title) ? file.Title : $"Ticket #{file.TicketNumber ?? file.TicketId?.ToString()}";
                stack.Children.Add(new TextBlock
                {
                    Text = $"📩 {displayTitle}",
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 13,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                });

                string statusText = file.DisplayStatus;
                string statusColor = file.Status.Equals("answered", StringComparison.OrdinalIgnoreCase)
                    ? "#2196F3"
                    : (file.Status.Equals("customer_reply", StringComparison.OrdinalIgnoreCase) ? "#FB8C00" : "#FF9800");

                var statusPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
                statusPanel.Children.Add(new TextBlock
                {
                    Text = string.Format(LanguageService.Get("Tune_StatusTicket"), statusText),
                    Foreground = Avalonia.Media.Brush.Parse(statusColor),
                    FontSize = 10,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                stack.Children.Add(statusPanel);

                if (file.TicketId.HasValue)
                {
                    int ticketId = file.TicketId.Value;
                    var viewBtn = new Button
                    {
                        Content = LanguageService.Get("Tune_ViewTicket"),
                        Background = Avalonia.Media.Brush.Parse("#FF9800"),
                        Foreground = Avalonia.Media.Brushes.Black,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        FontSize = 11,
                        Padding = new Thickness(8, 4),
                        Margin = new Thickness(0, 4, 0, 0),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
                    };

                    viewBtn.Click += (s, e) =>
                    {
                        var window = this.FindAncestorOfType<MainWindow>();
                        window?.Navigate(new TicketView(ticketId));
                    };

                    stack.Children.Add(viewBtn);
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
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                bool isCompleted = file.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);

                stack.Children.Add(new TextBlock
                {
                    Text = isCompleted ? LanguageService.Get("Tune_MapIdentified") : LanguageService.Get("Tune_IdentPending"),
                    Foreground = Avalonia.Media.Brush.Parse(isCompleted ? "#4DFF8A" : "#FFA500"),
                    FontSize = 10,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });
            }

            itemBorder.Child = stack;

            string fileHash = fileHashStr;
            itemBorder.PointerPressed += async (s, e) =>
            {
                if (_activeBorder != null)
                {
                    _activeBorder.Background = Avalonia.Media.Brush.Parse("#252525");
                }
                itemBorder.Background = Avalonia.Media.Brush.Parse("#353535");
                _activeBorder = itemBorder;

                if (!string.IsNullOrEmpty(fileHash))
                {
                    await SelectActiveFileAsync(fileHash);
                }
            };

            sidebar.Children.Add(itemBorder);
        }
    }

    private void SetCardsPendingState(string? stateText = null)
    {
        stateText ??= LanguageService.Get("Tune_StatusPending");

        var vehTitle = this.FindControl<TextBlock>("VehicleTitleText");
        var vehSub = this.FindControl<TextBlock>("VehicleSubtitleText");

        var vehProducer = this.FindControl<TextBlock>("VehProducerText");
        var vehModel = this.FindControl<TextBlock>("VehModelText");
        var vehYearChassis = this.FindControl<TextBlock>("VehYearChassisText");
        var vehBuildType = this.FindControl<TextBlock>("VehBuildTypeText");

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

        if (vehTitle != null) vehTitle.Text = LanguageService.Get("Tune_Title");
        if (vehSub != null)
        {
            vehSub.Text = stateText.Equals("Not Loaded", StringComparison.OrdinalIgnoreCase) || stateText.Equals("-", StringComparison.OrdinalIgnoreCase) || stateText == LanguageService.Get("Tune_StatusNotLoaded")
                ? LanguageService.Get("Tune_Subtitle")
                : string.Format(LanguageService.Get("Tune_StatusPrefix"), stateText);
        }

        if (vehProducer != null) vehProducer.Text = stateText;
        if (vehModel != null) vehModel.Text = stateText;
        if (vehYearChassis != null) vehYearChassis.Text = stateText;
        if (vehBuildType != null) vehBuildType.Text = stateText;

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

        string checkingText = LanguageService.Get("Tune_StatusChecking");
        SetCardsPendingState(checkingText);
        _isIdentifying = true;
        UpdateSummaryAndSaveButton();

        if (StatusText != null) StatusText.Text = checkingText;
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        var response = await ApiService.CheckStatusAsync(hash);
        if (response.Success)
        {
            if (response.Status == "completed" && response.Data != null)
            {
                _isIdentifying = false;
                PopulateEcuInfo(response.Data);
                var effectiveServices = response.Data.GetEffectiveServices();

                bool isFailed = string.Equals(response.Data.Status, "failed", StringComparison.OrdinalIgnoreCase);

                if (isFailed || !response.Data.IsSupported || effectiveServices == null || effectiveServices.Count == 0)
                {
                    if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusUnsupported");
                    if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF9800");
                    RenderUnsupportedEcuUi(response.Data);
                }
                else
                {
                    if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusIdentified");
                    if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                    RenderDynamicServices(effectiveServices);
                }

                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            }
            else
            {
                _isIdentifying = true;
                SetCardsPendingState(LanguageService.Get("Tune_StatusPending"));

                if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusProcessing");
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
                if (ServicesContainer != null) ServicesContainer.IsVisible = false;
            }
        }
        else
        {
            _isIdentifying = false;
            if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusFailed");
            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF4D4D");

            if (response.Data != null)
            {
                PopulateEcuInfo(response.Data);
                RenderUnsupportedEcuUi(response.Data);
            }
            else
            {
                SetCardsPendingState(LanguageService.Get("Tune_StatusFailed"));
            }
            if (ServicesContainer != null) ServicesContainer.IsVisible = true;
        }

        UpdateSummaryAndSaveButton();
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
            Text = LanguageService.Get("Tune_EcuNotFoundTitle"),
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brush.Parse("#FFB74D")
        });

        stack.Children.Add(new TextBlock
        {
            Text = LanguageService.Get("Tune_EcuNotFoundDesc"),
            FontSize = 12,
            Foreground = Avalonia.Media.Brush.Parse("#CCCCCC"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var submitBtn = new Button
        {
            Content = LanguageService.Get("Tune_SubmitTicketForEcu"),
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
            submitBtn.Content = LanguageService.Get("Tune_SendingTicket");

            string ecuName = !string.IsNullOrWhiteSpace(data?.EcuBrand) || !string.IsNullOrWhiteSpace(data?.EcuModel)
                ? $"{data?.EcuBrand} {data?.EcuModel}".Trim()
                : "Unidentified ECU File";

            string subject = $"Unsupported ECU Request: {ecuName}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Please check and add support for my uploaded ECU file:");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(data?.FileHash)) sb.AppendLine($"File Hash: {data.FileHash}");
            if (!string.IsNullOrWhiteSpace(data?.EcuBrand)) sb.AppendLine($"ECU Brand: {data.EcuBrand}");
            if (!string.IsNullOrWhiteSpace(data?.EcuModel)) sb.AppendLine($"ECU Model: {data.EcuModel}");
            if (!string.IsNullOrWhiteSpace(data?.HardwareId)) sb.AppendLine($"Hardware ID: {data.HardwareId}");
            if (!string.IsNullOrWhiteSpace(data?.SoftwareId)) sb.AppendLine($"Software ID: {data.SoftwareId}");

            var metadata = new
            {
                type = "ecu_unsupported",
                file_hash = data?.FileHash ?? "",
                ecu_brand = data?.EcuBrand ?? "",
                ecu_model = data?.EcuModel ?? "",
                hardware_id = data?.HardwareId ?? "",
                software_id = data?.SoftwareId ?? ""
            };

            var res = await ApiService.CreateTicketAsync(subject, sb.ToString().Trim(), metadata);
            if (res.Success)
            {
                submitBtn.Content = LanguageService.Get("Tune_TicketSent");
                submitBtn.Background = Avalonia.Media.Brush.Parse("#4CAF50");
                submitBtn.Foreground = Avalonia.Media.Brushes.White;
            }
            else
            {
                submitBtn.IsEnabled = true;
                submitBtn.Content = LanguageService.Get("Tune_RetryTicket");
            }
        };

        stack.Children.Add(submitBtn);
        border.Child = stack;
        panel.Children.Add(border);
    }

    private ServiceCategory CategorizeService(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return ServiceCategory.Features;

        string upper = name.ToUpperInvariant();
        if (upper.Contains("STAGE") || upper.Contains("ECO") || upper.Contains("TUNING") || upper.Contains("POWER") || upper.Contains("FLEX"))
        {
            return ServiceCategory.Performance;
        }

        if (upper.Contains("OFF") || upper.Contains("DELETE") || upper.Contains("REMOVE") || upper.Contains("DPF") ||
            upper.Contains("EGR") || upper.Contains("ADBLUE") || upper.Contains("SCR") || upper.Contains("CAT") ||
            upper.Contains("OPF") || upper.Contains("FLAP") || upper.Contains("VMAX") || upper.Contains("READINESS") ||
            upper.Contains("DTC") || upper.Contains("NOX") || upper.Contains("SWIRL") || upper.Contains("START-STOP") ||
            upper.Contains("LAMBDA"))
        {
            return ServiceCategory.Deletes;
        }

        return ServiceCategory.Features;
    }

    private void FilterButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            _activeFilter = tag;
            UpdateFilterButtonsUi();
            RenderServicesList();
        }
    }

    private void UpdateFilterButtonsUi()
    {
        var btnAll = this.FindControl<Button>("FilterAllButton");
        var btnPerf = this.FindControl<Button>("FilterPerfButton");
        var btnDeletes = this.FindControl<Button>("FilterDeletesButton");
        var btnFeatures = this.FindControl<Button>("FilterFeaturesButton");

        SetFilterStyle(btnAll, _activeFilter == "ALL");
        SetFilterStyle(btnPerf, _activeFilter == "PERFORMANCE");
        SetFilterStyle(btnDeletes, _activeFilter == "DELETES");
        SetFilterStyle(btnFeatures, _activeFilter == "FEATURES");
    }

    private void SetFilterStyle(Button? button, bool isActive)
    {
        if (button == null) return;
        if (isActive)
        {
            button.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
            button.Foreground = Avalonia.Media.Brush.Parse("#0F172A");
            button.FontWeight = Avalonia.Media.FontWeight.Bold;
        }
        else
        {
            button.Background = Avalonia.Media.Brush.Parse("#22242D");
            button.Foreground = Avalonia.Media.Brush.Parse("#CCCCCC");
            button.FontWeight = Avalonia.Media.FontWeight.SemiBold;
        }
    }

    private void RenderDynamicServices(List<ServiceDto>? services)
    {
        _currentServices = services;
        _serviceSelectionStates.Clear();
        _activeFilter = "ALL";
        UpdateFilterButtonsUi();
        RenderServicesList();
    }

    private void RenderServicesList()
    {
        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel == null) return;

        var selectedIds = new System.Collections.Generic.HashSet<int>();
        foreach (var child in panel.Children)
        {
            if (child is Border b && b.Child is StackPanel sp)
            {
                foreach (var innerChild in sp.Children)
                {
                    if (innerChild is ToggleSwitch t && t.IsChecked == true && t.Tag is int sId)
                    {
                        selectedIds.Add(sId);
                    }
                }
            }
        }

        panel.Children.Clear();

        if (_currentServices == null || _currentServices.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "No services available for this ECU.",
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 13,
                Margin = new Thickness(0, 15)
            });
            UpdateSummaryAndSaveButton();
            return;
        }

        foreach (var service in _currentServices)
        {
            var category = CategorizeService(service.Name);

            if (_activeFilter == "PERFORMANCE" && category != ServiceCategory.Performance) continue;
            if (_activeFilter == "DELETES" && category != ServiceCategory.Deletes) continue;
            if (_activeFilter == "FEATURES" && category != ServiceCategory.Features) continue;

            bool isSelected = _serviceSelectionStates.TryGetValue(service.Id, out bool sel) && sel;

            var cardBorder = new Border
            {
                Width = 230,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 12),
                Margin = new Thickness(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            var stack = new StackPanel { Spacing = 8 };

            // HEADER ROW WITH CATEGORY BADGE & SELECTED CHIP
            var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

            string catBg = "#1E2028";
            string catFg = "#CCCCCC";
            string catLabel = "OPTION";

            switch (category)
            {
                case ServiceCategory.Performance:
                    catBg = "#152438";
                    catFg = "#60A5FA";
                    catLabel = "🚀 STAGE";
                    break;
                case ServiceCategory.Deletes:
                    catBg = "#2E1C38";
                    catFg = "#C084FC";
                    catLabel = "🛡️ DELETE";
                    break;
                case ServiceCategory.Features:
                    catBg = "#332612";
                    catFg = "#FBBF24";
                    catLabel = "⚡ FEATURE";
                    break;
            }

            var catBadge = new Border
            {
                Background = Avalonia.Media.Brush.Parse(catBg),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = catLabel,
                    FontSize = 9,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brush.Parse(catFg)
                }
            };

            Grid.SetColumn(catBadge, 0);
            headerGrid.Children.Add(catBadge);

            var selectedBadge = new Border
            {
                Name = "SelectedBadge",
                Background = Avalonia.Media.Brush.Parse("#4DFF8A"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2),
                IsVisible = isSelected,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = LanguageService.Get("Tune_BadgeSelected"),
                    FontSize = 9,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brush.Parse("#0F172A")
                }
            };

            Grid.SetColumn(selectedBadge, 1);
            headerGrid.Children.Add(selectedBadge);

            stack.Children.Add(headerGrid);

            // TITLE
            var titleText = new TextBlock
            {
                Text = service.Name,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MinHeight = 36
            };
            stack.Children.Add(titleText);

            // FOOTER ROW WITH PRICE CHIP & TOGGLE SWITCH
            var footerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

            string priceDisplay;
            if (service.IsIncludedInSubscription)
            {
                priceDisplay = service.RemainingQuota.HasValue
                    ? string.Format(LanguageService.Get("Tune_IncludedLeft"), service.RemainingQuota.Value)
                    : LanguageService.Get("Tune_IncludedPlan");
            }
            else
            {
                priceDisplay = !string.IsNullOrWhiteSpace(service.Price) && service.Price != "Included" && service.Price != "Free"
                    ? $"🪙 {service.Price} CBT"
                    : LanguageService.Get("Tune_Included");
            }

            var priceText = new TextBlock
            {
                Text = priceDisplay,
                Foreground = Avalonia.Media.Brush.Parse(service.IsIncludedInSubscription ? "#4CAF50" : "#94A3B8"),
                FontSize = 11,
                FontWeight = service.IsIncludedInSubscription ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Medium,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(priceText, 0);
            footerGrid.Children.Add(priceText);

            var toggle = new ToggleSwitch
            {
                Tag = service,
                IsChecked = isSelected,
                OnContent = null,
                OffContent = null,
                Margin = new Thickness(0)
            };
            Grid.SetColumn(toggle, 1);
            footerGrid.Children.Add(toggle);
            stack.Children.Add(footerGrid);

            cardBorder.Child = stack;

            // Apply card visual selection state
            ApplyCardStyle(cardBorder, selectedBadge, isSelected);

            // Handlers
            toggle.IsCheckedChanged += (s, e) =>
            {
                bool checkedState = toggle.IsChecked == true;
                _serviceSelectionStates[service.Id] = checkedState;
                ApplyCardStyle(cardBorder, selectedBadge, checkedState);
                UpdateSummaryAndSaveButton();
            };

            cardBorder.PointerPressed += (s, e) =>
            {
                toggle.IsChecked = !toggle.IsChecked;
            };

            cardBorder.PointerEntered += (s, e) =>
            {
                if (toggle.IsChecked != true)
                {
                    cardBorder.BorderBrush = Avalonia.Media.Brush.Parse("#4A5164");
                }
            };

            cardBorder.PointerExited += (s, e) =>
            {
                if (toggle.IsChecked != true)
                {
                    cardBorder.BorderBrush = Avalonia.Media.Brush.Parse("#262933");
                }
            };

            panel.Children.Add(cardBorder);
        }

        UpdateSummaryAndSaveButton();
    }

    private void ApplyCardStyle(Border cardBorder, Border selectedBadge, bool isSelected)
    {
        if (isSelected)
        {
            cardBorder.Background = Avalonia.Media.Brush.Parse("#14281E");
            cardBorder.BorderBrush = Avalonia.Media.Brush.Parse("#4DFF8A");
            cardBorder.BorderThickness = new Thickness(1.5);
            selectedBadge.IsVisible = true;
        }
        else
        {
            cardBorder.Background = Avalonia.Media.Brush.Parse("#1A1C24");
            cardBorder.BorderBrush = Avalonia.Media.Brush.Parse("#262933");
            cardBorder.BorderThickness = new Thickness(1);
            selectedBadge.IsVisible = false;
        }
    }

    private void UpdateSummaryAndSaveButton()
    {
        int selectedCount = 0;
        double totalTokens = 0;

        if (_currentServices != null)
        {
            foreach (var service in _currentServices)
            {
                if (_serviceSelectionStates.TryGetValue(service.Id, out bool sel) && sel)
                {
                    selectedCount++;
                    if (double.TryParse(service.Price, out double priceVal))
                    {
                        totalTokens += priceVal;
                    }
                }
            }
        }

        var selectedText = this.FindControl<TextBlock>("SummarySelectedCountText");
        var totalText = this.FindControl<TextBlock>("SummaryTotalTokensText");

        if (selectedText != null) selectedText.Text = $"{selectedCount} Tune{(selectedCount == 1 ? "" : "s")}";
        if (totalText != null) totalText.Text = $"{totalTokens} CBT";

        if (SaveButton != null)
        {
            /* Quota limit check disabled
            bool hasReachedDailyLimit = ApiService.CurrentUser?.HasReachedDailyLimit == true;
            */

            if (_isIdentifying)
            {
                SaveButton.Content = LanguageService.Get("Tune_IdentifyingEcu");
                SaveButton.Background = Avalonia.Media.Brush.Parse("#252525");
                SaveButton.Foreground = Avalonia.Media.Brush.Parse("#888888");
                SaveButton.IsEnabled = false;
            }
            else if (string.IsNullOrEmpty(_pendingFileHash))
            {
                /*
                if (hasReachedDailyLimit)
                {
                    SaveButton.Content = LanguageService.Get("Tune_DailyLimitReachedShort");
                    SaveButton.Background = Avalonia.Media.Brush.Parse("#252525");
                    SaveButton.Foreground = Avalonia.Media.Brush.Parse("#888888");
                    SaveButton.IsEnabled = false;
                }
                else
                */
                {
                    SaveButton.Content = LanguageService.Get("Tune_SelectEcuFile");
                    SaveButton.Background = Avalonia.Media.Brushes.White;
                    SaveButton.Foreground = Avalonia.Media.Brush.Parse("#141414");
                    SaveButton.IsEnabled = true;
                }
            }
            else
            {
                string orderSelected = LanguageService.Get("Tune_OrderSelected");
                string selectedLabel = LanguageService.Get("Tune_Selected");
                if (selectedCount > 0)
                {
                    SaveButton.Content = $"{orderSelected} ({selectedCount} {selectedLabel} • {totalTokens} CBT)";
                }
                else
                {
                    SaveButton.Content = orderSelected;
                }
                SaveButton.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                SaveButton.Foreground = Avalonia.Media.Brushes.Black;
                SaveButton.IsEnabled = true;
            }
        }
    }

    private async Task OpenFilePickerAndUploadAsync()
    {
        if (_isProcessing) return;

        /* Quota limit check disabled
        if (ApiService.CurrentUser?.HasReachedDailyLimit == true)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window window)
            {
                int limit = ApiService.CurrentUser.OrderLimit ?? 0;
                await MessageBox(window, string.Format(LanguageService.Get("Tune_DailyLimitReached"), limit));
            }
            return;
        }
        */

        var topLevelPicker = TopLevel.GetTopLevel(this);
        if (topLevelPicker == null) return;

        var files = await topLevelPicker.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LanguageService.Get("Tune_SelectEcuBinaryFile"),
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

    private void OnFileDragOver(object? sender, DragEventArgs e)
    {
        /* Quota limit check disabled
        if (ApiService.CurrentUser?.HasReachedDailyLimit == true)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }
        */

        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void OnFileDrop(object? sender, DragEventArgs e)
    {
        if (_isProcessing) return;

        /* Quota limit check disabled
        if (ApiService.CurrentUser?.HasReachedDailyLimit == true)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window window)
            {
                int limit = ApiService.CurrentUser.OrderLimit ?? 0;
                await MessageBox(window, string.Format(LanguageService.Get("Tune_DailyLimitReached"), limit));
            }
            return;
        }
        */

        #pragma warning disable CS0618
        var files = e.Data.GetFiles();
        #pragma warning restore CS0618
        if (files != null)
        {
            var file = files.FirstOrDefault(f =>
                f.Path.LocalPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                f.Path.LocalPath.EndsWith(".hex", StringComparison.OrdinalIgnoreCase) ||
                f.Path.LocalPath.EndsWith(".ori", StringComparison.OrdinalIgnoreCase) ||
                f.Path.LocalPath.EndsWith(".dec", StringComparison.OrdinalIgnoreCase));

            if (file != null)
            {
                await ProcessAndUploadFileAsync(file.Path.LocalPath);
            }
        }
    }

    private async Task ProcessAndUploadFileAsync(string filePath)
    {
        _isProcessing = true;
        _isIdentifying = true;
        string uploadingText = LanguageService.Get("Tune_StatusUploading");
        SetCardsPendingState(uploadingText);
        UpdateSummaryAndSaveButton();

        if (StatusText != null) StatusText.Text = uploadingText;
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
                _isIdentifying = false;
                PopulateEcuInfo(response.Data);
                RenderDynamicServices(response.Data.GetEffectiveServices());
                if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusIdentified");
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#4DFF8A");
                if (ServicesContainer != null) ServicesContainer.IsVisible = true;
            }
            else
            {
                _isIdentifying = true;
                SetCardsPendingState(LanguageService.Get("Tune_StatusPending"));

                if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusProcessing");
                if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FFA500");
                if (ServicesContainer != null) ServicesContainer.IsVisible = false;
            }
        }
        else
        {
            _isIdentifying = false;
            _pendingFileHash = null;
            if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusFailed");
            if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brush.Parse("#FF4D4D");
            SetCardsPendingState(LanguageService.Get("Tune_StatusFailed"));
            if (ServicesContainer != null) ServicesContainer.IsVisible = false;
        }

        UpdateSummaryAndSaveButton();
    }

    private async void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        /* Quota limit check disabled
        if (ApiService.CurrentUser?.HasReachedDailyLimit == true)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window window)
            {
                int limit = ApiService.CurrentUser.OrderLimit ?? 0;
                await MessageBox(window, string.Format(LanguageService.Get("Tune_DailyLimitReached"), limit));
            }
            return;
        }
        */

        if (string.IsNullOrEmpty(_pendingFileHash))
        {
            await OpenFilePickerAndUploadAsync();
            return;
        }

        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel == null) return;

        var selectedServiceIds = new List<int>();
        var selectedServiceNames = new List<string>();

        if (_currentServices != null && _currentServices.Count > 0)
        {
            foreach (var service in _currentServices)
            {
                if (_serviceSelectionStates.TryGetValue(service.Id, out bool sel) && sel)
                {
                    selectedServiceIds.Add(service.Id);
                    if (!string.IsNullOrEmpty(service.Name)) selectedServiceNames.Add(service.Name);
                }
            }
        }
        else
        {
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
        }

        if (selectedServiceIds.Count == 0 && selectedServiceNames.Count == 0)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var window = topLevel as Window;
            if (window != null)
            {
                await MessageBox(window, LanguageService.Get("Tune_SelectServiceRequired"));
            }
            return;
        }

        var saveButton = sender as Button;
        if (saveButton != null)
        {
            saveButton.IsEnabled = false;
            saveButton.Content = LanguageService.Get("Tune_Downloading");
        }

        string orderedHash = _pendingFileHash;

        try
        {
            var (success, message, orderId) = await ApiService.CreateOrderAsync(orderedHash, selectedServiceIds, selectedServiceNames);

            if (!success)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window win)
                {
                    await MessageBox(win, message);
                }
                return;
            }

            // Start global tracking (handles polling, persistent indicator across tabs, and native Save dialog)
            OrderProcessingManager.StartTrackingOrder(orderedHash, orderId);

            ResetWorkspace();
            _ = ApiService.FetchProfileAsync();
            _ = LoadProcessingFilesAsync();
        }
        catch (Exception ex)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window win)
            {
                await MessageBox(win, $"Error: {ex.Message}");
            }
        }
        finally
        {
            if (saveButton != null)
            {
                saveButton.IsEnabled = true;
                UpdateSummaryAndSaveButton();
            }
        }
    }

    private async Task<(bool Success, string Message)> DownloadAndSaveFileAsync(
        string downloadUrl,
        string suggestedFileName,
        Action<string>? onProgress = null)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not Window window)
            return (false, "Window not available");

        try
        {
            var saveFile = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = LanguageService.Get("Tune_SavePickerTitle"),
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "bin",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Binary File (*.bin)")
                    {
                        Patterns = new[] { "*.bin" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files (*.*)")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (saveFile == null)
                return (false, string.Empty); // User canceled picker

            onProgress?.Invoke(LanguageService.Get("Tune_Downloading"));

            using var stream = await saveFile.OpenWriteAsync();
            var progress = new System.Progress<double>(p =>
            {
                onProgress?.Invoke($"{LanguageService.Get("Tune_Downloading")} {p:F0}%...");
            });

            return await ApiService.DownloadFileToStreamAsync(downloadUrl, stream, progress);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void NewFileButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ResetWorkspace();
    }

    private void ResetWorkspace()
    {
        _isIdentifying = false;
        _pendingFileHash = null;
        _renderedFileHash = null;
        _currentServices = null;
        _serviceSelectionStates.Clear();
        _activeFilter = "ALL";
        if (_activeBorder != null)
        {
            _activeBorder.Background = Avalonia.Media.Brush.Parse("#252525");
            _activeBorder = null;
        }

        SetCardsPendingState(LanguageService.Get("Tune_StatusNotLoaded"));

        if (StatusText != null) StatusText.Text = LanguageService.Get("Tune_StatusReady");
        if (StatusDot != null) StatusDot.Background = Avalonia.Media.Brushes.Gray;
        if (ServicesContainer != null) ServicesContainer.IsVisible = false;

        var panel = this.FindControl<WrapPanel>("DynamicServicesPanel");
        if (panel != null) panel.Children.Clear();

        UpdateFilterButtonsUi();
        UpdateSummaryAndSaveButton();
    }

    private async Task MessageBox(Window window, string message)
    {
        var dialog = new Window
        {
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://ADIapp/Assets/sidebar_logo.png"))),
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
