using Avalonia.Controls;
using Avalonia.Threading;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class AppShellView : UserControl
{
    public AppShellView()
    {
        InitializeComponent();

        OrderProcessingManager.Initialize();
    }

    /// <summary>
    /// Navigates to a page view inside the app shell content slot.
    /// </summary>
    public void NavigatePage(Control page)
    {
        PageContent.Content = page;

        Sidebar?.HighlightForPage(page);

        if (TopUserPanel != null)
        {
            TopUserPanel.IsVisible = page is not TuneView && page is not TicketView;
        }
    }
}

