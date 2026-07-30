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
        
        UpdateLocalizedText();
        LanguageService.LanguageChanged += OnLanguageChanged;

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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
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
        var title = this.FindControl<TextBlock>("AccountTitleBlock");
        if (title != null) title.Text = LanguageService.Get("Account_Title");

        var subtitle = this.FindControl<TextBlock>("AccountSubtitleBlock");
        if (subtitle != null) subtitle.Text = LanguageService.Get("Account_Subtitle");

        var fnLabel = this.FindControl<TextBlock>("FirstNameLabelBlock");
        if (fnLabel != null) fnLabel.Text = LanguageService.Get("Account_FirstName");

        var lnLabel = this.FindControl<TextBlock>("LastNameLabelBlock");
        if (lnLabel != null) lnLabel.Text = LanguageService.Get("Account_LastName");

        var emailLabel = this.FindControl<TextBlock>("EmailLabelBlock");
        if (emailLabel != null) emailLabel.Text = LanguageService.Get("Account_Email");

        var phoneLabel = this.FindControl<TextBlock>("PhoneLabelBlock");
        if (phoneLabel != null) phoneLabel.Text = LanguageService.Get("Account_Phone");

        var saveBtn = this.FindControl<TextBlock>("SaveBtnTextBlock");
        if (saveBtn != null) saveBtn.Text = LanguageService.Get("Account_Save");

        if (ApiService.CurrentUser != null)
        {
            var helloUserBlock = this.FindControl<TextBlock>("HelloUserBlock");
            if (helloUserBlock != null)
            {
                helloUserBlock.Text = $"{LanguageService.Get("TopPanel_Hello")} {ApiService.CurrentUser.FirstName} {ApiService.CurrentUser.LastName}.".Trim();
            }
        }
    }

    private void PopulateFields()
    {
        if (ApiService.CurrentUser != null)
        {
            var helloUserBlock = this.FindControl<TextBlock>("HelloUserBlock");
            if (helloUserBlock != null)
            {
                helloUserBlock.Text = $"{LanguageService.Get("TopPanel_Hello")} {ApiService.CurrentUser.FirstName} {ApiService.CurrentUser.LastName}.".Trim();
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