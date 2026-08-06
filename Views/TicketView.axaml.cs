using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia;
using Avalonia.Threading;
using ADIapp.Services;
using ADIapp.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ADIapp.Views;

public partial class TicketView : UserControl
{
    private List<TicketDto> _tickets = new();
    private TicketDto? _selectedTicket;
    private int? _initialTicketId;

    public TicketView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (s, e) =>
        {
            WebSocketManager.TicketUpdated += OnTicketUpdated;
            _ = LoadTicketsAsync();
        };

        this.DetachedFromVisualTree += (s, e) =>
        {
            WebSocketManager.TicketUpdated -= OnTicketUpdated;
        };
    }

    public TicketView(int initialTicketId) : this()
    {
        _initialTicketId = initialTicketId;
    }

    private void OnTicketUpdated()
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await LoadTicketsAsync();
        });
    }

    private async Task LoadTicketsAsync()
    {
        _tickets = await ApiService.GetTicketsAsync();
        RenderTicketList();

        if (_initialTicketId.HasValue)
        {
            var match = _tickets.Find(t => t.Id == _initialTicketId.Value);
            if (match != null)
            {
                var full = await ApiService.GetTicketDetailsAsync(match.Id);
                await SelectTicketAsync(full ?? match);
                _initialTicketId = null;
                return;
            }
        }

        if (_selectedTicket != null)
        {
            var updated = await ApiService.GetTicketDetailsAsync(_selectedTicket.Id);
            if (updated != null)
            {
                await SelectTicketAsync(updated);
            }
        }
        else if (_tickets.Count > 0)
        {
            await SelectTicketAsync(_tickets[0]);
        }
        else
        {
            ClearDetailView();
        }
    }

    private void RenderTicketList()
    {
        if (TicketListPanel == null) return;
        TicketListPanel.Children.Clear();

        if (_tickets == null || _tickets.Count == 0)
        {
            TicketListPanel.Children.Add(new TextBlock
            {
                Text = "No tickets yet. Click '+ New Ticket' to create one.",
                Foreground = Brush.Parse("#888888"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40)
            });
            return;
        }

        foreach (var ticket in _tickets)
        {
            bool isSelected = _selectedTicket?.Id == ticket.Id;
            var border = new Border
            {
                Background = Brush.Parse(isSelected ? "#2B2B2B" : "#202020"),
                BorderBrush = Brush.Parse(isSelected ? "#3A86FF" : "#333333"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            var stack = new StackPanel { Spacing = 4 };

            var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var numText = new TextBlock
            {
                Text = ticket.TicketNumber,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse("#80D8FF")
            };
            Grid.SetColumn(numText, 0);

            string statusColor = ticket.Status switch
            {
                "open" => "#E53935",
                "customer_reply" => "#FB8C00",
                "answered" => "#1E88E5",
                "closed" => "#43A047",
                _ => "#757575"
            };

            var statusBadge = new Border
            {
                Background = Brush.Parse(statusColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2),
                Child = new TextBlock
                {
                    Text = ticket.DisplayStatus,
                    FontSize = 9,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                }
            };
            Grid.SetColumn(statusBadge, 1);

            headerGrid.Children.Add(numText);
            headerGrid.Children.Add(statusBadge);
            stack.Children.Add(headerGrid);

            stack.Children.Add(new TextBlock
            {
                Text = ticket.Subject,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            if (ticket.LatestMessage != null)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = ticket.LatestMessage.Content,
                    FontSize = 12,
                    Foreground = Brush.Parse("#AAAAAA"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            border.Child = stack;
            border.PointerPressed += async (s, e) => await SelectTicketAsync(ticket);

            TicketListPanel.Children.Add(border);
        }
    }

    private async Task SelectTicketAsync(TicketDto ticket)
    {
        _selectedTicket = ticket;
        RenderTicketList();

        if (TicketNumberText != null) TicketNumberText.Text = ticket.TicketNumber;
        if (TicketSubjectText != null) TicketSubjectText.Text = ticket.Subject;
        if (TicketStatusText != null) TicketStatusText.Text = ticket.DisplayStatus;

        if (TicketStatusBadge != null)
        {
            string statusColor = ticket.Status switch
            {
                "open" => "#E53935",
                "customer_reply" => "#FB8C00",
                "answered" => "#1E88E5",
                "closed" => "#43A047",
                _ => "#757575"
            };
            TicketStatusBadge.Background = Brush.Parse(statusColor);
        }

        if (ticket.Messages == null || ticket.Messages.Count == 0)
        {
            var fullTicket = await ApiService.GetTicketDetailsAsync(ticket.Id);
            if (fullTicket != null && fullTicket.Messages != null)
            {
                ticket = fullTicket;
                _selectedTicket = fullTicket;
            }
        }

        RenderMessages(ticket.Messages);
    }

    private void ClearDetailView()
    {
        _selectedTicket = null;
        if (TicketNumberText != null) TicketNumberText.Text = "#TK-0000";
        if (TicketSubjectText != null) TicketSubjectText.Text = "No tickets available";
        if (TicketStatusText != null) TicketStatusText.Text = "NONE";
        if (MessagesPanel != null) MessagesPanel.Children.Clear();
    }

    private void RenderMessages(List<TicketMessageDto>? messages)
    {
        if (MessagesPanel == null) return;
        MessagesPanel.Children.Clear();

        if (messages == null || messages.Count == 0)
        {
            MessagesPanel.Children.Add(new TextBlock
            {
                Text = "No messages in this ticket thread yet.",
                Foreground = Brush.Parse("#888888"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40)
            });
            return;
        }

        foreach (var msg in messages)
        {
            bool isAdmin = msg.Sender?.ToLower() == "admin";

            var bubble = new Border
            {
                Background = Brush.Parse(isAdmin ? "#2B2B2B" : "#1A3A5C"),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 4),
                HorizontalAlignment = isAdmin ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                MaxWidth = 550
            };

            var stack = new StackPanel { Spacing = 4 };

            stack.Children.Add(new TextBlock
            {
                Text = isAdmin ? "💬 Technical Support (Admin)" : "👤 You",
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse(isAdmin ? "#4DFF8A" : "#80D8FF")
            });

            stack.Children.Add(new TextBlock
            {
                Text = msg.Content,
                FontSize = 13,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrEmpty(msg.CreatedAt))
            {
                string displayDate = msg.CreatedAt;
                if (DateTime.TryParse(msg.CreatedAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedDate))
                {
                    displayDate = parsedDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }
                else if (DateTime.TryParse(msg.CreatedAt, out var fallbackDate))
                {
                    displayDate = fallbackDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }

                stack.Children.Add(new TextBlock
                {
                    Text = displayDate,
                    FontSize = 9,
                    Foreground = Brush.Parse("#888888"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            bubble.Child = stack;
            MessagesPanel.Children.Add(bubble);
        }

        MessagesScrollViewer?.ScrollToEnd();
    }

    private async void SendReplyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedTicket == null || ReplyInput == null || string.IsNullOrWhiteSpace(ReplyInput.Text))
            return;

        string content = ReplyInput.Text.Trim();
        if (SendReplyButton != null) SendReplyButton.IsEnabled = false;

        var res = await ApiService.SendTicketReplyAsync(_selectedTicket.Id, content);
        if (SendReplyButton != null) SendReplyButton.IsEnabled = true;

        if (res.Success)
        {
            ReplyInput.Text = string.Empty;
            await LoadTicketsAsync();
        }
        else
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window window)
            {
                await MessageBox(window, $"Failed to send reply: {res.Message}");
            }
        }
    }

    private void NewTicketButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TicketModalErrorText != null) TicketModalErrorText.IsVisible = false;
        if (NewTicketOverlay != null) NewTicketOverlay.IsVisible = true;
    }

    private void CancelNewTicket_Click(object? sender, RoutedEventArgs e)
    {
        if (TicketModalErrorText != null) TicketModalErrorText.IsVisible = false;
        if (NewTicketOverlay != null) NewTicketOverlay.IsVisible = false;
        if (NewSubjectInput != null) NewSubjectInput.Text = string.Empty;
        if (NewContentInput != null) NewContentInput.Text = string.Empty;
    }

    private async void SubmitNewTicket_Click(object? sender, RoutedEventArgs e)
    {
        if (NewSubjectInput == null || NewContentInput == null) return;
        if (TicketModalErrorText != null) TicketModalErrorText.IsVisible = false;

        string subject = NewSubjectInput.Text?.Trim() ?? "";
        string content = NewContentInput.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(content))
        {
            if (TicketModalErrorText != null)
            {
                TicketModalErrorText.Text = "Please enter both subject and message content.";
                TicketModalErrorText.IsVisible = true;
            }
            return;
        }

        if (sender is Button submitBtn) submitBtn.IsEnabled = false;

        var res = await ApiService.CreateTicketAsync(subject, content);
        if (sender is Button btn) btn.IsEnabled = true;

        if (res.Success && res.Ticket != null)
        {
            CancelNewTicket_Click(sender, e);
            await LoadTicketsAsync();
            await SelectTicketAsync(res.Ticket);
        }
        else
        {
            if (TicketModalErrorText != null)
            {
                TicketModalErrorText.Text = string.IsNullOrEmpty(res.Message) ? "Failed to create ticket. Please try again." : res.Message;
                TicketModalErrorText.IsVisible = true;
            }
        }
    }

    private async Task MessageBox(Window window, string message)
    {
        var dialog = new Window
        {
            Width = 320,
            Height = 140,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(20),
                Background = Brush.Parse("#1F1F1F"),
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        await dialog.ShowDialog(window);
    }
}