using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ADIapp.Views;

public partial class TopUserPanelView : UserControl
{
    public TopUserPanelView()
    {
        InitializeComponent();
    }

    private async void Bell_Click(object? sender, PointerPressedEventArgs e)
    {
        var window = this.VisualRoot as Window;

        if (window != null)
        {
            await MessageBox(window, "Your credit has been recharged");
        }
    }

    private void User_Click(object? sender, PointerPressedEventArgs e)
    {
        Menu.IsVisible = !Menu.IsVisible;
    }

    private async System.Threading.Tasks.Task MessageBox(Window window, string message)
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
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black),
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
