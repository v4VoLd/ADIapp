using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using ADIapp.Helpers;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class InfoView : UserControl
{
    public InfoView()
    {
        InitializeComponent();
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        UpdateLocalizedText();
        LanguageService.LanguageChanged += OnLanguageChanged;

        PopulateCompanyInfo();

        var info = await ApiService.FetchCompanyInfoAsync();
        if (info != null)
        {
            PopulateCompanyInfo();
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

    private void PopulateCompanyInfo()
    {
        var info = ApiService.CachedCompanyInfo;
        if (info == null) return;

        var email = this.FindControl<TextBlock>("EmailValueBlock");
        if (email != null && !string.IsNullOrWhiteSpace(info.Email))
        {
            email.Text = info.Email;
        }

        var phone = this.FindControl<TextBlock>("PhoneValueBlock");
        if (phone != null && !string.IsNullOrWhiteSpace(info.Phone))
        {
            phone.Text = info.Phone;
        }

        var phone2 = this.FindControl<TextBlock>("Phone2ValueBlock");
        if (phone2 != null)
        {
            string secondary = string.Join(" / ", new[] { info.Phone2, info.Phone3 }
                .Where(p => !string.IsNullOrWhiteSpace(p)));
            if (!string.IsNullOrWhiteSpace(secondary))
            {
                phone2.Text = secondary;
                phone2.IsVisible = true;
            }
            else
            {
                phone2.IsVisible = false;
            }
        }

        var whatsapp = this.FindControl<TextBlock>("WhatsappValueBlock");
        if (whatsapp != null && !string.IsNullOrWhiteSpace(info.Whatsapp))
        {
            whatsapp.Text = info.Whatsapp;
        }

        var address = this.FindControl<TextBlock>("AddressValueBlock");
        if (address != null && !string.IsNullOrWhiteSpace(info.Address))
        {
            address.Text = info.Address;
        }

        var social = this.FindControl<TextBlock>("SocialValueBlock");
        if (social != null && !string.IsNullOrWhiteSpace(info.Facebook))
        {
            social.Text = info.Facebook;
        }

        var website = this.FindControl<TextBlock>("WebsiteValueBlock");
        if (website != null && !string.IsNullOrWhiteSpace(info.Website))
        {
            website.Text = info.Website;
        }
    }

    private void UpdateLocalizedText()
    {
        var title = this.FindControl<TextBlock>("InfoTitleBlock");
        if (title != null) title.Text = LanguageService.Get("Info_Title");

        var subtitle = this.FindControl<TextBlock>("InfoSubtitleBlock");
        if (subtitle != null) subtitle.Text = LanguageService.Get("Info_Subtitle");

        var website = this.FindControl<TextBlock>("WebsiteTileBlock");
        if (website != null) website.Text = LanguageService.Get("Info_Website");

        var email = this.FindControl<TextBlock>("EmailTileBlock");
        if (email != null) email.Text = LanguageService.Get("Info_Email");

        var phone = this.FindControl<TextBlock>("PhoneTileBlock");
        if (phone != null) phone.Text = LanguageService.Get("Info_Phone");

        var whatsapp = this.FindControl<TextBlock>("WhatsappTileBlock");
        if (whatsapp != null) whatsapp.Text = LanguageService.Get("Info_Whatsapp");

        var address = this.FindControl<TextBlock>("AddressTileBlock");
        if (address != null) address.Text = LanguageService.Get("Info_Address");

        var social = this.FindControl<TextBlock>("SocialTileBlock");
        if (social != null) social.Text = LanguageService.Get("Info_Social");
    }

    // ─────────────────────────────────────────────────────────────
    // Click / Pointer Action Handlers
    // ─────────────────────────────────────────────────────────────

    private void WebsiteTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string website = !string.IsNullOrWhiteSpace(info?.Website) ? info.Website : "adi-performance.com";
        UrlHelper.OpenUrl(website);
    }

    private void EmailTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string email = !string.IsNullOrWhiteSpace(info?.Email) ? info.Email : "contact@adi-performance.com";
        UrlHelper.OpenMailto(email);
    }

    private void PhoneTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string phone = !string.IsNullOrWhiteSpace(info?.Phone) ? info.Phone : "+213559808501";
        UrlHelper.OpenTel(phone);
    }

    private void Phone2_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string phone = !string.IsNullOrWhiteSpace(info?.Phone2) ? info.Phone2 : info?.Phone3 ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            UrlHelper.OpenTel(phone);
            e.Handled = true;
        }
    }

    private void WhatsappTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string whatsapp = !string.IsNullOrWhiteSpace(info?.Whatsapp) ? info.Whatsapp : "+213770957227";
        UrlHelper.OpenWhatsApp(whatsapp);
    }

    private void AddressTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string address = !string.IsNullOrWhiteSpace(info?.Address) ? info.Address : "Cité Djilali Ahmed, Staoueli, Alger";
        UrlHelper.OpenMap(address);
    }

    private void SocialTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var info = ApiService.CachedCompanyInfo;
        string facebook = !string.IsNullOrWhiteSpace(info?.Facebook) ? info.Facebook : "https://www.facebook.com/ADIPerformance/";
        UrlHelper.OpenUrl(facebook);
    }
}