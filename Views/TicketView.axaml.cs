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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateLocalizedText();
        LanguageService.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LanguageService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        var title = this.FindControl<TextBlock>("TicketTitleBlock");
        if (title != null) title.Text = LanguageService.Get("Ticket_Title");

        var subtitle = this.FindControl<TextBlock>("TicketSubtitleBlock");
        if (subtitle != null) subtitle.Text = LanguageService.Get("Ticket_Subtitle");
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

        if (_selectedTicket != null)
        {
            var updated = await ApiService.GetTicketDetailsAsync(_selectedTicket.Id);
            if (updated != null)
            {
                SelectTicket(updated);
            }
        }
        else if (_tickets.Count > 0)
        {
            SelectTicket(_tickets[0]);
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
                    Text = ticket.Status.ToUpper(),
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
            border.PointerPressed += (s, e) => SelectTicket(ticket);

            TicketListPanel.Children.Add(border);
        }
    }

    private void SelectTicket(TicketDto ticket)
    {
        _selectedTicket = ticket;
        RenderTicketList();

        if (TicketNumberText != null) TicketNumberText.Text = ticket.TicketNumber;
        if (TicketSubjectText != null) TicketSubjectText.Text = ticket.Subject;
        if (TicketStatusText != null) TicketStatusText.Text = ticket.Status.ToUpper();

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
            var emptyStack = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 60)
            };

            emptyStack.Children.Add(new TextBlock
            {
                Text = "💬",
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            emptyStack.Children.Add(new TextBlock
            {
                Text = "No Messages Yet",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            emptyStack.Children.Add(new TextBlock
            {
                Text = "Type a message below to start communicating with technical support.",
                FontSize = 13,
                Foreground = Brush.Parse("#A0A0B0"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            MessagesPanel.Children.Add(emptyStack);
            return;
        }

        foreach (var msg in messages)
        {
            bool isAdmin = msg.Sender?.ToLower() == "admin";

            var bubble = new Border
            {
                Background = Brush.Parse(isAdmin ? "#252525" : "#1E3A5F"),
                BorderBrush = Brush.Parse(isAdmin ? "#3D3D3D" : "#2A5A8F"),
                BorderThickness = new Thickness(1),
                CornerRadius = isAdmin ? new CornerRadius(14, 14, 14, 3) : new CornerRadius(14, 14, 3, 14),
                Padding = new Thickness(16, 12),
                Margin = new Thickness(0, 4),
                HorizontalAlignment = isAdmin ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                MaxWidth = 580
            };

            var stack = new StackPanel { Spacing = 6 };

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
                stack.Children.Add(new TextBlock
                {
                    Text = msg.CreatedAt,
                    FontSize = 10,
                    Foreground = Brush.Parse("#A0A0B0"),
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
        ReplyInput.Text = string.Empty;

        var res = await ApiService.SendTicketReplyAsync(_selectedTicket.Id, content);
        if (res.Success)
        {
            await LoadTicketsAsync();
        }
    }

    private void NewTicketButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NewTicketOverlay != null) NewTicketOverlay.IsVisible = true;
    }

    private void CancelNewTicket_Click(object? sender, RoutedEventArgs e)
    {
        if (NewTicketOverlay != null) NewTicketOverlay.IsVisible = false;
        if (NewSubjectInput != null) NewSubjectInput.Text = string.Empty;
        if (NewContentInput != null) NewContentInput.Text = string.Empty;
    }

    private async void SubmitNewTicket_Click(object? sender, RoutedEventArgs e)
    {
        if (NewSubjectInput == null || NewContentInput == null) return;

        string subject = NewSubjectInput.Text?.Trim() ?? "";
        string content = NewContentInput.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(content))
            return;

        var res = await ApiService.CreateTicketAsync(subject, content);
        if (res.Success && res.Ticket != null)
        {
            CancelNewTicket_Click(sender, e);
            await LoadTicketsAsync();
            SelectTicket(res.Ticket);
        }
    }
}