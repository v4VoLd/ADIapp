using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ADIapp.Services;
using System.Threading.Tasks;

namespace ADIapp.Views;

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string message, string? title = null) : this()
    {
        var titleBlock = this.FindControl<TextBlock>("DialogTitleText");
        var msgBlock = this.FindControl<TextBlock>("DialogMessageText");
        var okBtn = this.FindControl<Button>("OkButton");

        string resolvedTitle = !string.IsNullOrWhiteSpace(title) 
            ? title 
            : LanguageService.Get("Dialog_Notice");

        if (titleBlock != null) titleBlock.Text = resolvedTitle;
        if (msgBlock != null) msgBlock.Text = message;
        if (okBtn != null) okBtn.Content = LanguageService.Get("Dialog_Ok");
    }

    public static async Task ShowAsync(Window? owner, string message, string? title = null)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new MessageDialog(message, title);
            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                dialog.Show();
            }
        });
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
