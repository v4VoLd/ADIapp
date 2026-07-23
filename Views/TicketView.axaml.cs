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
            MessagesPanel.Children.Add(new TextBlock
            {
                Text = "No support tickets or messages yet.",
                Foreground = Brush.Parse("#888888"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40)
            });
            return;
        }

        foreach (var msg in messages)
        {
            bool isAdmin = msg.Sender?.ToUpper() == "ADMIN";

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
                stack.Children.Add(new TextBlock
                {
                    Text = msg.CreatedAt,
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