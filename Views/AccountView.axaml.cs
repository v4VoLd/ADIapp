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
}