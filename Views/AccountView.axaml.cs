using Avalonia.Controls;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class AccountView : UserControl
{
    public AccountView()
    {
        InitializeComponent();
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        
        // Populate with currently cached profile data immediately
        PopulateFields();

        // Fetch fresh profile data from the Laravel API profile controller
        var (success, _) = await ApiService.FetchProfileAsync();
        if (success)
        {
            // Re-populate with updated data from the backend
            PopulateFields();
        }
    }

    private void PopulateFields()
    {
        if (ApiService.CurrentUser != null)
        {
            var helloUserBlock = this.FindControl<TextBlock>("HelloUserBlock");
            if (helloUserBlock != null)
            {
                helloUserBlock.Text = $"Hello, {ApiService.CurrentUser.FirstName} {ApiService.CurrentUser.LastName}.".Trim();
            }

            var firstNameBox = this.FindControl<TextBox>("FirstNameBox");
            var lastNameBox = this.FindControl<TextBox>("LastNameBox");
            if (firstNameBox != null)
            {
                firstNameBox.Text = ApiService.CurrentUser.FirstName;
            }
            if (lastNameBox != null)
            {
                lastNameBox.Text = ApiService.CurrentUser.LastName;
            }

            var emailBox = this.FindControl<TextBox>("EmailBox");
            if (emailBox != null)
            {
                emailBox.Text = ApiService.CurrentUser.Email;
            }

            var phoneBox = this.FindControl<TextBox>("PhoneBox");
            if (phoneBox != null)
            {
                phoneBox.Text = ApiService.CurrentUser.PhoneNumber ?? string.Empty;
            }
        }
    }

    private async void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var firstNameBox = this.FindControl<TextBox>("FirstNameBox");
        var lastNameBox = this.FindControl<TextBox>("LastNameBox");
        var phoneBox = this.FindControl<TextBox>("PhoneBox");
        var saveButton = this.FindControl<Button>("SaveButton");

        string firstName = firstNameBox?.Text?.Trim() ?? string.Empty;
        string lastName = lastNameBox?.Text?.Trim() ?? string.Empty;
        string phone = phoneBox?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            NotificationService.AddNotification("account_error", "First name and Last name are required.", "error");
            return;
        }

        if (saveButton != null) saveButton.IsEnabled = false;

        var (success, msg) = await ApiService.UpdateProfileAsync(firstName, lastName, phone);

        if (saveButton != null) saveButton.IsEnabled = true;

        if (success)
        {
            PopulateFields();
            NotificationService.AddNotification($"account_saved_{System.Guid.NewGuid()}", "Account profile updated successfully!", "info");
        }
        else
        {
            NotificationService.AddNotification($"account_failed_{System.Guid.NewGuid()}", $"Failed to update account: {msg}", "error");
        }
    }
}