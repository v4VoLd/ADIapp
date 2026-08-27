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
        var headerTitle = this.FindControl<TextBlock>("SupportTicketsHeaderBlock");
        if (headerTitle != null) headerTitle.Text = LanguageService.Get("Ticket_HeaderTitle");

        if (NewTicketButton != null) NewTicketButton.Content = LanguageService.Get("Ticket_NewTicket");

        if (_selectedTicket == null && TicketSubjectText != null)
        {
            TicketSubjectText.Text = LanguageService.Get("Ticket_SelectPrompt");
        }

        if (ReplyInput != null) ReplyInput.Watermark = LanguageService.Get("Ticket_ReplyWatermark");
        if (SendReplyButton != null) SendReplyButton.Content = LanguageService.Get("Ticket_SendReply");

        var modalTitle = this.FindControl<TextBlock>("NewTicketModalTitleBlock");
        if (modalTitle != null) modalTitle.Text = LanguageService.Get("Ticket_CreateTitle");

        var subjectLabel = this.FindControl<TextBlock>("SubjectLabelBlock");
        if (subjectLabel != null) subjectLabel.Text = LanguageService.Get("Ticket_SubjectLabel");

        if (NewSubjectInput != null) NewSubjectInput.Watermark = LanguageService.Get("Ticket_SubjectPlaceholder");

        var contentLabel = this.FindControl<TextBlock>("ContentLabelBlock");
        if (contentLabel != null) contentLabel.Text = LanguageService.Get("Ticket_ContentLabel");

        if (NewContentInput != null) NewContentInput.Watermark = LanguageService.Get("Ticket_ContentPlaceholder");

        var cancelBtn = this.FindControl<Button>("CancelNewTicketButton");
        if (cancelBtn != null) cancelBtn.Content = LanguageService.Get("Ticket_Cancel");

        var submitBtn = this.FindControl<Button>("SubmitNewTicketButton");
        if (submitBtn != null) submitBtn.Content = LanguageService.Get("Ticket_SubmitTicket");
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
                    Text = GetLocalizedStatus(ticket.Status, ticket.DisplayStatus),
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
        if (TicketStatusText != null) TicketStatusText.Text = GetLocalizedStatus(ticket.Status, ticket.DisplayStatus);

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

    private string GetLocalizedStatus(string? status, string? fallbackDisplay)
    {
        if (string.IsNullOrEmpty(status)) return fallbackDisplay ?? "";
        string key = status.ToUpper();
        string translated = LanguageService.Get(key);
        return translated != key ? translated : (fallbackDisplay ?? status.ToUpper());
    }

    private void ClearDetailView()
    {
        _selectedTicket = null;
        if (TicketNumberText != null) TicketNumberText.Text = "#TK-0000";
        if (TicketSubjectText != null) TicketSubjectText.Text = LanguageService.Get("Ticket_NoTickets");
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
        if (SendReplyButton != null)
        {
            SendReplyButton.IsEnabled = false;
            SendReplyButton.Content = "Sending...";
        }

        try
        {
            var res = await ApiService.SendTicketReplyAsync(_selectedTicket.Id, content);

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
        finally
        {
            if (SendReplyButton != null)
            {
                SendReplyButton.IsEnabled = true;
                SendReplyButton.Content = LanguageService.Get("Ticket_Send") != "Ticket_Send" ? LanguageService.Get("Ticket_Send") : "Send Reply";
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
                TicketModalErrorText.Text = LanguageService.Get("Ticket_SubjectAndMessageRequired");
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
                TicketModalErrorText.Text = string.IsNullOrEmpty(res.Message) ? LanguageService.Get("Ticket_FailedToCreate") : res.Message;
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