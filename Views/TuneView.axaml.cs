using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Threading.Tasks;

namespace ADIapp.Views;

public partial class TuneView : UserControl
{
    private TextBlock? SavedText;

    public TuneView()
    {
        InitializeComponent();
        SavedText = this.FindControl<TextBlock>("SavedText");
    }
    private async void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SavedText != null)
        {
            SavedText.IsVisible = true;
            await Task.Delay(1500);
            SavedText.IsVisible = false;
        }
    }

}
