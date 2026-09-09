using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ADIapp.Services;
using ADIapp.Models;
using ADIapp.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADIapp.Views;

public partial class OrderHistoryView : UserControl
{
    private List<OrderHistoryItemDto> _orders = new();
    private List<OrderHistoryItemDto> _tickets = new();
    private string _activeFilter = "All";

    public OrderHistoryView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (s, e) =>
        {
            WebSocketManager.OrderUpdated += OnOrderUpdated;
            LanguageService.LanguageChanged += OnLanguageChanged;
            UpdateLocalizedText();

            _ = LoadHistoryAsync();
        };

        this.DetachedFromVisualTree += (s, e) =>
        {
            WebSocketManager.OrderUpdated -= OnOrderUpdated;
            LanguageService.LanguageChanged -= OnLanguageChanged;
        };
    }

    private void OnLanguageChanged()
    {
        Dispatcher.UIThread.Post(() => UpdateLocalizedText());
    }

    private void UpdateLocalizedText()
    {
        var TabAll = this.FindControl<Button>("TabAll");
        var TabCompleted = this.FindControl<Button>("TabCompleted");
        var TabCanceled = this.FindControl<Button>("TabCanceled");
        var TabEcuTickets = this.FindControl<Button>("TabEcuTickets");
        var TitleBlock = this.FindControl<TextBlock>("TitleBlock");
        var SubtitleBlock = this.FindControl<TextBlock>("SubtitleBlock");
        if (TitleBlock != null) TitleBlock.Text = LanguageService.Get("Orders_Title");
        if (SubtitleBlock != null) SubtitleBlock.Text = LanguageService.Get("Orders_Subtitle");
        if (TabAll != null) TabAll.Content = LanguageService.Get("Orders_All");
        if (TabCompleted != null) TabCompleted.Content = LanguageService.Get("Orders_Completed");
        if (TabCanceled != null) TabCanceled.Content = LanguageService.Get("Orders_Canceled");
        if (TabEcuTickets != null) TabEcuTickets.Content = LanguageService.Get("Orders_EcuTickets");
    }

    private void OnOrderUpdated()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _ = LoadHistoryAsync();
        });
    }

    private async Task LoadHistoryAsync()
    {
        _ = ApiService.FetchProfileAsync();
        var response = await ApiService.GetOrderHistoryAsync();
        if (response != null)
        {
            _orders = response.Orders ?? new List<OrderHistoryItemDto>();
            _tickets = response.EcuTickets ?? new List<OrderHistoryItemDto>();
        }
        else
        {
            _orders = new List<OrderHistoryItemDto>();
            _tickets = new List<OrderHistoryItemDto>();
        }

        RenderList();
    }

    private void FilterTab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string filter)
        {
            _activeFilter = filter;

            UpdateTabButtonStyles();
            RenderList();
        }
    }

    private void UpdateTabButtonStyles()
    {
        var tabs = new[] { TabAll, TabCompleted, TabCanceled, TabEcuTickets };
        foreach (var tab in tabs)
        {
            if (tab == null) continue;
            bool isActive = string.Equals(tab.Tag as string, _activeFilter, StringComparison.OrdinalIgnoreCase);
            if (isActive)
            {
                if (!tab.Classes.Contains("activeTab")) tab.Classes.Add("activeTab");
            }
            else
            {
                tab.Classes.Remove("activeTab");
            }
        }
    }

    private void RenderList()
    {
        if (OrderListPanel == null) return;
        OrderListPanel.Children.Clear();

        bool hasItems = false;

        // Render Orders based on filter
        if (_activeFilter is "All" or "Completed" or "Canceled")
        {
            var filteredOrders = _orders.Where(o =>
            {
                if (_activeFilter == "Completed") return o.IsCompleted;
                if (_activeFilter == "Canceled") return o.IsCanceled;
                return true; // All
            }).ToList();

            foreach (var order in filteredOrders)
            {
                hasItems = true;
                OrderListPanel.Children.Add(CreateOrderCard(order));
            }
        }

        // Render Pending ECU Tickets based on filter
        if (_activeFilter is "All" or "EcuTickets")
        {
            foreach (var ticket in _tickets)
            {
                hasItems = true;
                OrderListPanel.Children.Add(CreateTicketCard(ticket));
            }
        }

        if (!hasItems)
        {
            OrderListPanel.Children.Add(new TextBlock
            {
                Text = GetEmptyMessageForFilter(_activeFilter),
                Foreground = Brush.Parse("#888888"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 50)
            });
        }
    }

    private string GetEmptyMessageForFilter(string filter)
    {
        return filter switch
        {
            "Completed" => LanguageService.Get("Orders_NoCompletedOrders"),
            "Canceled" => LanguageService.Get("Orders_NoCanceledOrders"),
            "EcuTickets" => LanguageService.Get("Orders_NoEcuTickets"),
            _ => LanguageService.Get("Orders_NoOrderHistory")
        };
    }

    private Border CreateOrderCard(OrderHistoryItemDto order)
    {
        var card = new Border
        {
            Background = Brush.Parse("#252525"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            BorderBrush = Brush.Parse("#333333"),
            BorderThickness = new Thickness(1)
        };

        var mainStack = new StackPanel { Spacing = 10 };

        // Header Grid: Order ID & Date on left, Status Badge on right
        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        var titleStack = new StackPanel { Spacing = 3 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        titleRow.Children.Add(new TextBlock
        {
            Text = $" {LanguageService.Get("Orders_Title")} #{order.Id}",
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        });

        if (order.IsOriginal)
        {
            titleRow.Children.Add(new Border
            {
                Background = Brush.Parse("#0369A1"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2),
                Child = new TextBlock
                {
                    Text = $"📁 {LanguageService.Get("Order_OriginalBadge")}",
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                }
            });
        }
        titleStack.Children.Add(titleRow);

        if (order.IsOriginal && !string.IsNullOrEmpty(order.ReadHardware))
        {
            titleStack.Children.Add(new TextBlock
            {
                Text = $"⚡ {LanguageService.Get("Tune_ColReader")}: {order.ReadHardware}",
                FontSize = 11,
                Foreground = Brush.Parse("#38BDF8"),
                FontWeight = FontWeight.SemiBold
            });
        }

        if (!string.IsNullOrEmpty(order.CreatedAt))
        {
            titleStack.Children.Add(new TextBlock
            {
                Text = FormatDate(order.CreatedAt),
                FontSize = 11,
                Foreground = Brush.Parse("#888888")
            });
        }
        Grid.SetColumn(titleStack, 0);

        // Status Badge
        string statusKey = order.Status.Replace("_", " ").ToUpper();
        string statusText = LanguageService.Get(statusKey);
        string statusColor = order.Status.ToLower() switch
        {
            "completed" or "finished" => "#4DFF8A",
            "canceled" => "#FF5252",
            "processing" => "#2196F3",
            _ => "#FFA500"
        };

        var statusBadge = new Border
        {
            Background = Brush.Parse(statusColor),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = statusText,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = order.Status.ToLower() == "completed" ? Brushes.Black : Brushes.White
            }
        };
        Grid.SetColumn(statusBadge, 1);

        headerGrid.Children.Add(titleStack);
        headerGrid.Children.Add(statusBadge);
        mainStack.Children.Add(headerGrid);

        // Services list
        if (order.Services != null && order.Services.Count > 0)
        {
            var servicesPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            foreach (var svc in order.Services)
            {
                servicesPanel.Children.Add(new Border
                {
                    Background = Brush.Parse("#353535"),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4),
                    Margin = new Thickness(0, 0, 6, 4),
                    Child = new TextBlock
                    {
                        Text = $"{svc.Name} ({svc.Price} {LanguageService.Get("Token_Unit")})",
                        FontSize = 11,
                        Foreground = Brush.Parse("#E0E0E0")
                    }
                });
            }
            mainStack.Children.Add(servicesPanel);
        }
        else if (order.IsOriginal)
        {
            var servicesPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            servicesPanel.Children.Add(new Border
            {
                Background = Brush.Parse("#0C4A6E"),
                BorderBrush = Brush.Parse("#0284C7"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 6, 4),
                Child = new TextBlock
                {
                    Text = !string.IsNullOrEmpty(order.ReadHardware) 
                        ? $"📁 Factory Original Binary ({order.ReadHardware})" 
                        : "📁 Factory Original Binary",
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brush.Parse("#BAE6FD")
                }
            });
            mainStack.Children.Add(servicesPanel);
        }

        // Bottom Info Row: Total Tokens & Download Action
        var bottomGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 4, 0, 0) };

        var priceText = new TextBlock
        {
            Text = $"{LanguageService.Get("Orders_Total")}: {order.TotalPrice} {LanguageService.Get("Total_Cost_Tokens")}",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#FFB74D"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(priceText, 0);
        bottomGrid.Children.Add(priceText);

        if (order.IsCompleted)
        {
            if (order.IsDownloadExpired)
            {
                var expiredBtn = new Button
                {
                    Content = LanguageService.Get("Tune_DownloadExpired"),
                    Background = Brush.Parse("#333333"),
                    Foreground = Brush.Parse("#888888"),
                    FontWeight = FontWeight.SemiBold,
                    FontSize = 12,
                    Padding = new Thickness(12, 6),
                    CornerRadius = new CornerRadius(6),
                    IsEnabled = false
                };
                Grid.SetColumn(expiredBtn, 1);
                bottomGrid.Children.Add(expiredBtn);
            }
            else if (!string.IsNullOrEmpty(order.DownloadUrl) || order.Id > 0)
            {
                string downloadUrl = order.DownloadUrl ?? $"{AppConfig.BaseUrl}/order/download/{order.Id}";
                string fileName = OrderProcessingManager.GenerateSuggestedFileName(order);

                var downloadBtn = new Button
                {
                    Content = LanguageService.Get("Download_ModFile"),
                    Background = Brush.Parse("#4DFF8A"),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Padding = new Thickness(12, 6),
                    CornerRadius = new CornerRadius(6)
                };

                downloadBtn.Click += async (s, e) =>
                {
                    downloadBtn.IsEnabled = false;
                    downloadBtn.Content = LanguageService.Get("Tune_Downloading");

                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel is Window window)
                    {
                        var saveFile = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                        {
                            Title = LanguageService.Get("Tune_SavePickerTitle"),
                            SuggestedFileName = fileName,
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

                        if (saveFile != null)
                        {
                            bool isCompleted = false;
                            using var stream = await saveFile.OpenWriteAsync();
                            var progress = new System.Progress<double>(p =>
                            {
                                if (!isCompleted)
                                {
                                    downloadBtn.Content = $"{LanguageService.Get("Tune_Downloading")} {p:F0}%...";
                                }
                            });

                            var (success, msg) = await ApiService.DownloadFileToStreamAsync(downloadUrl, stream, progress);
                            isCompleted = true;

                            if (success)
                            {
                                downloadBtn.Content = LanguageService.Get("Tune_Downloaded");

                                _ = Task.Run(async () =>
                                {
                                    await Task.Delay(2000);
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        downloadBtn.IsEnabled = true;
                                        downloadBtn.Content = LanguageService.Get("Download_ModFile");
                                    });
                                });
                            }
                            else
                            {
                                downloadBtn.IsEnabled = true;
                                downloadBtn.Content = LanguageService.Get("Tune_RetryDownload");
                            }
                        }
                        else
                        {
                            downloadBtn.IsEnabled = true;
                            downloadBtn.Content = LanguageService.Get("Download_ModFile");
                        }
                    }
                };

                Grid.SetColumn(downloadBtn, 1);
                bottomGrid.Children.Add(downloadBtn);
            }
        }

        mainStack.Children.Add(bottomGrid);
        card.Child = mainStack;
        return card;
    }

    private Border CreateTicketCard(OrderHistoryItemDto ticket)
    {
        var card = new Border
        {
            Background = Brush.Parse("#252525"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            BorderBrush = Brush.Parse("#FF9800"),
            BorderThickness = new Thickness(1)
        };

        var mainStack = new StackPanel { Spacing = 10 };

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        var titleStack = new StackPanel { Spacing = 2 };
        string titleText = !string.IsNullOrEmpty(ticket.Title) ? ticket.Title : string.Format(LanguageService.Get("Tune_UnfoundEcuRequest"), ticket.TicketNumber);
        titleStack.Children.Add(new TextBlock
        {
            Text = $"📩 {titleText}",
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        });

        if (!string.IsNullOrEmpty(ticket.CreatedAt))
        {
            titleStack.Children.Add(new TextBlock
            {
                Text = FormatDate(ticket.CreatedAt),
                FontSize = 11,
                Foreground = Brush.Parse("#888888")
            });
        }
        Grid.SetColumn(titleStack, 0);

        string rawStatus = ticket.Status.Replace("_", " ").ToUpper();
        string statusText = string.Format(LanguageService.Get("Tune_StatusTicket"), rawStatus);
        var statusBadge = new Border
        {
            Background = Brush.Parse("#FF9800"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = statusText,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Black
            }
        };
        Grid.SetColumn(statusBadge, 1);

        headerGrid.Children.Add(titleStack);
        headerGrid.Children.Add(statusBadge);
        mainStack.Children.Add(headerGrid);

        // Details Panel (Hardware/Software IDs)
        var infoStack = new StackPanel { Spacing = 4 };
        if (!string.IsNullOrEmpty(ticket.EcuBrand) || !string.IsNullOrEmpty(ticket.EcuModel))
        {
            infoStack.Children.Add(new TextBlock
            {
                Text = $"ECU: {ticket.EcuBrand} {ticket.EcuModel}".Trim(),
                FontSize = 12,
                Foreground = Brush.Parse("#CCCCCC")
            });
        }
        if (!string.IsNullOrEmpty(ticket.HardwareId))
        {
            infoStack.Children.Add(new TextBlock
            {
                Text = $"HW ID: {ticket.HardwareId}",
                FontSize = 11,
                Foreground = Brush.Parse("#AAAAAA")
            });
        }
        mainStack.Children.Add(infoStack);

        // Action Button: Navigate to ticket view thread
        int ticketId = ticket.Id;
        var viewBtn = new Button
        {
            Content = LanguageService.Get("Tune_OpenDiscussion"),
            Background = Brush.Parse("#FF9800"),
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            Padding = new Thickness(12, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            CornerRadius = new CornerRadius(6)
        };

        viewBtn.Click += (s, e) =>
        {
            var window = this.FindAncestorOfType<MainWindow>();
            window?.Navigate(new TicketView(ticketId));
        };

        mainStack.Children.Add(viewBtn);

        card.Child = mainStack;
        return card;
    }

    private string FormatDate(string isoDate)
    {
        if (DateTimeOffset.TryParse(isoDate, out var dto))
        {
            return dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        if (DateTime.TryParse(isoDate, out var fallbackDt))
        {
            return fallbackDt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        return isoDate;
    }
}
