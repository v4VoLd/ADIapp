using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia;
using ADIapp.Services;
using ADIapp.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ADIapp.Views;

public partial class TicketView : UserControl
{
    public TicketView()
    {
        InitializeComponent();
        _ = LoadMessagesAsync();
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

        if (MessageInput != null) MessageInput.Watermark = LanguageService.Get("Ticket_Placeholder");
        if (SendButton != null) SendButton.Content = LanguageService.Get("Ticket_Send");
    }

    private async Task LoadMessagesAsync()
    {
        var messages = await ApiService.GetSupportMessagesAsync();
        RenderMessages(messages);
    }

    private void RenderMessages(List<SupportMessageDto> messages)
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
            bool isAdmin = msg.Sender?.ToUpper() == "ADMIN";

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

    private async void SendButton_Click(object? sender, RoutedEventArgs e)
    {
        if (MessageInput == null || string.IsNullOrWhiteSpace(MessageInput.Text))
            return;

        string content = MessageInput.Text.Trim();
        MessageInput.Text = string.Empty;

        var res = await ApiService.SendSupportMessageAsync(content);
        if (res.Success)
        {
            await LoadMessagesAsync();
        }
    }
}