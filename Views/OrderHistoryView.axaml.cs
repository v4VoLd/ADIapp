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

    private Avalonia.Threading.DispatcherTimer? _pollTimer;

    public OrderHistoryView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (s, e) =>
        {
            WebSocketManager.OrderUpdated += OnOrderUpdated;

            _pollTimer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _pollTimer.Tick += (sender, args) => _ = LoadHistoryAsync();
            _pollTimer.Start();

            _ = LoadHistoryAsync();
        };

        this.DetachedFromVisualTree += (s, e) =>
        {
            WebSocketManager.OrderUpdated -= OnOrderUpdated;
            _pollTimer?.Stop();
        };
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
            "Completed" => "No completed orders found.",
            "Canceled" => "No canceled orders found.",
            "EcuTickets" => "No pending ECU support tickets found.",
            _ => "No order or request history found."
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

        var titleStack = new StackPanel { Spacing = 2 };
        titleStack.Children.Add(new TextBlock
        {
            Text = $"⚡ Order #{order.Id}",
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        });

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
        string statusText = order.Status.Replace("_", " ").ToUpper();
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
                        Text = $"{svc.Name} ({svc.Price} Token)",
                        FontSize = 11,
                        Foreground = Brush.Parse("#E0E0E0")
                    }
                });
            }
            mainStack.Children.Add(servicesPanel);
        }

        // Bottom Info Row: Total Tokens & Download Action
        var bottomGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 4, 0, 0) };

        var priceText = new TextBlock
        {
            Text = $"Total: {order.TotalPrice} Token(s)",
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
                    Content = "⏰ Download Expired",
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
                string fileName = order.FileSent ?? $"order_{order.Id}_mod.bin";

                var downloadBtn = new Button
                {
                    Content = "⬇ Download Mod File",
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
                            var progress = new System.Progress<double>(p =>
                            {
                                downloadBtn.Content = $"Downloading {p:F0}%...";
                            });

                            var (success, msg) = await ApiService.DownloadFileToStreamAsync(downloadUrl, stream, progress);
                            if (success)
                            {
                                downloadBtn.Content = "✓ Downloaded";
                                NotificationService.AddNotification($"download_done_{order.Id}", $"File download completed: {fileName}", "info");
                            }
                            else
                            {
                                downloadBtn.IsEnabled = true;
                                downloadBtn.Content = "Retry Download";
                            }
                        }
                        else
                        {
                            downloadBtn.IsEnabled = true;
                            downloadBtn.Content = "⬇ Download Mod File";
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
        string titleText = !string.IsNullOrEmpty(ticket.Title) ? ticket.Title : $"Unfound ECU Request #{ticket.TicketNumber}";
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

        string statusText = $"TICKET • {ticket.Status.Replace("_", " ").ToUpper()}";
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
            Content = "💬 Open Support Discussion",
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
        if (DateTime.TryParse(isoDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
        {
            return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        if (DateTime.TryParse(isoDate, out var fallbackDt))
        {
            return fallbackDt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        return isoDate;
    }
}
