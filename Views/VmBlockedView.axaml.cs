using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ADIapp.Views;

public partial class VmBlockedView : UserControl
{
    public VmBlockedView()
    {
        InitializeComponent();
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
}
