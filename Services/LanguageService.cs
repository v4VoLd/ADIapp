using System;
using System.Collections.Generic;
using System.IO;

namespace ADIapp.Services;

public static class LanguageService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ADIapp",
        "language.txt"
    );

    public static string CurrentLanguage { get; private set; } = LoadSavedLanguage();

    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Settings_Title"] = "Settings",
            ["Settings_Language"] = "Language",
            ["Sidebar_Home"] = "Home",
            ["Sidebar_Settings"] = "Settings",
            ["Sidebar_Tickets"] = "Tickets",
            ["Sidebar_Notifications"] = "Notifications",
            ["Sidebar_Info"] = "Info",
            ["Sidebar_Logout"] = "Logout",
            ["Sidebar_LicenseExp"] = "License Expiration",
            ["Home_Welcome"] = "Welcome to\nADI Performance",
            ["Home_Desc"] = "Next-generation ECU tuning,\nfile service management and automotive performance diagnostics.",
            ["Home_GetStarted"] = "Get Started ➔",
            ["Home_LearnMore"] = "📖 How It Works",
            ["Home_QuickAccess"] = "Quick Access",
            ["Home_QuickAccessDesc"] = "Explore core tools and manage your performance files",
            ["Home_EcuTuning"] = "ECU Tuning Service",
            ["Home_EcuDesc"] = "Upload original ECU files and request custom remaps for Stage 1, Stage 2, DPF & EGR solutions.",
            ["Home_SupportTickets"] = "Support & Tickets",
            ["Home_SupportDesc"] = "Communicate directly with our master engineers for custom modifications and technical assistance.",
            ["Home_PlatformInfo"] = "Platform Information",
            ["Home_PlatformDesc"] = "Check active license details, hardware compatibility, and platform documentation.",
            ["TopPanel_Hello"] = "Hello,",
            ["TopPanel_Token"] = "Token:",
            ["TopPanel_Account"] = "Account",
            ["TopPanel_Logout"] = "Logout",
            ["TopPanel_Notifications"] = "Notifications",
            ["TopPanel_ClearAll"] = "Clear All",
            ["TopPanel_SeeAll"] = "See All Notifications ➔",
            ["TopPanel_TokenMenu"] = "Token Balance",
            ["Orders_Title"] = "Orders",
            ["Orders_Subtitle"] = "Track past tuning orders, completed/canceled files, and pending ECU support requests.",
            ["Notifications_Title"] = "Notifications & Alerts",
            ["Notifications_Subtitle"] = "View and manage all system updates, order alerts, and messages.",
            ["Notifications_MarkAll"] = "Mark All Read",
            ["Notifications_NoNotifs"] = "No notifications yet",
            ["Logout_Title"] = "Log Out",
            ["Logout_Message"] = "Are you sure you want to log out? You will need to sign back in to access your account.",
            ["Logout_Cancel"] = "Cancel",
            ["Logout_Confirm"] = "Log Out",
            ["Login_Welcome"] = "Welcome Back",
            ["Login_Subtitle"] = "Sign in to access your ADI Performance account",
            ["Login_Username"] = "Username or Email",
            ["Login_Password"] = "Password",
            ["Login_Submit"] = "Sign In ➔",
            ["Login_SigningIn"] = "Signing in...",
            ["Login_NoAccount"] = "Don't have an account? Sign Up",
            ["Login_PasswordPlaceholder"] = "Enter password",
            ["Login_UsernamePlaceholder"] = "Enter username or email",
            ["Account_Title"] = "Account Profile",
            ["Account_Subtitle"] = "Manage your personal information and contact details",
            ["Account_FirstName"] = "First name",
            ["Account_LastName"] = "Last name",
            ["Account_Email"] = "Email address",
            ["Account_Phone"] = "Phone number",
            ["Account_Save"] = "Save Changes",
            ["Token_Title"] = "Token Credits",
            ["Token_Subtitle"] = "Your current available service tokens balance",
            ["Token_AvailableBalance"] = "Available Balance",
            ["Info_Badge"] = "HOW IT WORKS",
            ["Info_Title"] = "How To Use ADI Performance",
            ["Info_Subtitle"] = "4 simple steps to tune and modify your vehicle file in seconds",
            ["Info_Step1_Title"] = "📁 Upload Your File",
            ["Info_Step1_Desc"] = "Click New Upload on the left sidebar and choose your original vehicle binary file.",
            ["Info_Step2_Title"] = "🔍 Instant Vehicle Check",
            ["Info_Step2_Desc"] = "Our system automatically detects your vehicle specs, engine displacement, and ECU info.",
            ["Info_Step3_Title"] = "⚡ Select Options",
            ["Info_Step3_Desc"] = "Toggle your desired performance stages, delete options, or special features with 1 click.",
            ["Info_Step4_Title"] = "💾 Save & Download",
            ["Info_Step4_Desc"] = "Click Save Order to confirm your selection and download your ready-to-use tuned file!",
            ["Info_UnderstandBtn"] = "I Understand ➔",
            ["Info_Website"] = "Our Website",
            ["Info_Updates"] = "Updates",
            ["Info_News"] = "News & Portal",
            ["Info_Phone"] = "Phone Support",
            ["Ticket_Title"] = "Support Tickets & Messages",
            ["Ticket_Subtitle"] = "Communicate directly with technical support regarding ECU additions and orders.",
            ["Ticket_Placeholder"] = "Type your support message...",
            ["Ticket_Send"] = "Send Message",
            ["Settings_Subtitle"] = "Customize application preferences and display language",
            ["Tune_Title"] = "Vehicle & ECU Information",
            ["Tune_Subtitle"] = "Upload a binary file or select an active task to inspect specifications.",
            ["Tune_Back"] = "← Back",
            ["Tune_ActiveTasks"] = "Active Tasks",
            ["Tune_NewUpload"] = "+ New Upload",
            ["Tune_NoActiveTasks"] = "No active tasks",
            ["Tune_VehSpecs"] = "VEHICLE SPECS",
            ["Tune_Producer"] = "PRODUCER",
            ["Tune_Model"] = "MODEL",
            ["Tune_YearChassis"] = "YEAR / CHASSIS",
            ["Tune_BuildType"] = "BUILD / TYPE",
            ["Tune_EngSpecs"] = "ENGINE SPECS",
            ["Tune_NameType"] = "NAME / TYPE",
            ["Tune_Displacement"] = "DISPLACEMENT",
            ["Tune_Output"] = "OUTPUT (PS / KW)",
            ["Tune_Emission"] = "EMISSION",
            ["Tune_Transmission"] = "TRANSMISSION",
            ["Tune_EcuSpecs"] = "ECU SPECS",
            ["Tune_ProdBuild"] = "PRODUCER / BUILD",
            ["Tune_HwNr"] = "STAGE / HW NR",
            ["Tune_ProdNr"] = "PART / PROD NR",
            ["Tune_SwVersion"] = "SW VERSION",
            ["Tune_SwSize"] = "SW SIZE",
            ["Tune_AvailableTunes"] = "AVAILABLE DATABASE TUNES & MODIFICATIONS",
            ["Tune_OriginalFile"] = "Original file",
            ["Tune_Save"] = "Save"
        },
        ["fr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Settings_Title"] = "Paramètres",
            ["Settings_Language"] = "Langue",
            ["Sidebar_Home"] = "Accueil",
            ["Sidebar_Settings"] = "Paramètres",
            ["Sidebar_Tickets"] = "Support",
            ["Sidebar_Info"] = "À propos",
            ["Sidebar_Logout"] = "Déconnexion",
            ["Sidebar_LicenseExp"] = "Expiration de licence",
            ["Home_Welcome"] = "Bienvenue sur\nADI Performance",
            ["Home_Desc"] = "Service de reprogrammation ECU nouvelle génération,\ngestion de fichiers et diagnostic automobile.",
            ["Home_GetStarted"] = "Commencer ➔",
            ["Home_LearnMore"] = "📖 Comment ça marche",
            ["Home_QuickAccess"] = "Accès Rapide",
            ["Home_QuickAccessDesc"] = "Explorez les outils principaux et gérez vos fichiers de performance",
            ["Home_EcuTuning"] = "Service Tuning ECU",
            ["Home_EcuDesc"] = "Téléchargez vos fichiers ECU originaux et demandez des cartographies sur mesure (Stage 1, Stage 2, DPF & EGR).",
            ["Home_SupportTickets"] = "Support & Tickets",
            ["Home_SupportDesc"] = "Communiquez directement avec nos ingénieurs pour vos demandes spécifiques et assistance technique.",
            ["Home_PlatformInfo"] = "Informations Plateforme",
            ["Home_PlatformDesc"] = "Vérifiez votre licence active, la compatibilité matérielle et la documentation.",
            ["TopPanel_Hello"] = "Bonjour,",
            ["TopPanel_Token"] = "Jetons :",
            ["TopPanel_Account"] = "Mon Compte",
            ["TopPanel_Logout"] = "Déconnexion",
            ["TopPanel_Notifications"] = "Notifications",
            ["TopPanel_ClearAll"] = "Tout effacer",
            ["TopPanel_SeeAll"] = "Voir toutes les notifications ➔",
            ["TopPanel_TokenMenu"] = "Solde Jetons",
            ["Orders_Title"] = "Commandes",
            ["Orders_Subtitle"] = "Suivez vos commandes de reprogrammation passées et demandes de support ECU.",
            ["Notifications_Title"] = "Notifications & Alertes",
            ["Notifications_Subtitle"] = "Consultez et gérez toutes les mises à jour système et alertes.",
            ["Notifications_MarkAll"] = "Tout marquer comme lu",
            ["Notifications_NoNotifs"] = "Aucune notification",
            ["Logout_Title"] = "Déconnexion",
            ["Logout_Message"] = "Êtes-vous sûr de vouloir vous déconnecter ? Vous devrez vous réauthentifier pour accéder à votre compte.",
            ["Logout_Cancel"] = "Annuler",
            ["Logout_Confirm"] = "Se déconnecter",
            ["Login_Welcome"] = "Bienvenue",
            ["Login_Subtitle"] = "Connectez-vous à votre compte ADI Performance",
            ["Login_Username"] = "Nom d'utilisateur ou e-mail",
            ["Login_Password"] = "Mot de passe",
            ["Login_Submit"] = "Se connecter ➔",
            ["Login_SigningIn"] = "Connexion...",
            ["Login_NoAccount"] = "Vous n'avez pas de compte ? S'inscrire",
            ["Login_PasswordPlaceholder"] = "Entrez votre mot de passe",
            ["Login_UsernamePlaceholder"] = "Entrez votre nom d'utilisateur ou e-mail",
            ["Account_Title"] = "Profil du Compte",
            ["Account_Subtitle"] = "Gérez vos informations personnelles et vos coordonnées",
            ["Account_FirstName"] = "Prénom",
            ["Account_LastName"] = "Nom",
            ["Account_Email"] = "Adresse e-mail",
            ["Account_Phone"] = "Numéro de téléphone",
            ["Account_Save"] = "Enregistrer les modifications",
            ["Token_Title"] = "Crédits Jetons",
            ["Token_Subtitle"] = "Votre solde actuel de jetons de service disponibles",
            ["Token_AvailableBalance"] = "Solde Disponible",
            ["Info_Badge"] = "COMMENT ÇA MARCHE",
            ["Info_Title"] = "Comment utiliser ADI Performance",
            ["Info_Subtitle"] = "4 étapes simples pour reprogrammer votre fichier véhicule en quelques secondes",
            ["Info_Step1_Title"] = "📁 Téléchargez votre fichier",
            ["Info_Step1_Desc"] = "Cliquez sur Nouveau Fichier dans le menu de gauche et choisissez votre fichier binaire d'origine.",
            ["Info_Step2_Title"] = "🔍 Détection automatique",
            ["Info_Step2_Desc"] = "Notre système détecte automatiquement les données de votre véhicule et du calculateur.",
            ["Info_Step3_Title"] = "⚡ Sélectionnez vos options",
            ["Info_Step3_Desc"] = "Activez vos stages de puissance, suppressions d'options ou options spéciales en 1 clic.",
            ["Info_Step4_Title"] = "💾 Validez et Téléchargez",
            ["Info_Step4_Desc"] = "Cliquez sur Enregistrer pour valider votre commande et télécharger votre fichier prêt à l'emploi !",
            ["Info_UnderstandBtn"] = "J'ai compris ➔",
            ["Info_Website"] = "Notre Site Web",
            ["Info_Updates"] = "Mises à jour",
            ["Info_News"] = "Actualités & Portail",
            ["Info_Phone"] = "Support Téléphonique",
            ["Ticket_Title"] = "Tickets de Support & Messages",
            ["Ticket_Subtitle"] = "Communiquez directement avec le support technique pour les ajouts d'ECU et commandes.",
            ["Ticket_Placeholder"] = "Tapez votre message de support...",
            ["Ticket_Send"] = "Envoyer le message",
            ["Settings_Subtitle"] = "Personnalisez les préférences de l'application et la langue d'affichage",
            ["Tune_Title"] = "Informations Véhicule & ECU",
            ["Tune_Subtitle"] = "Téléchargez un fichier binaire ou sélectionnez une tâche pour inspecter ses spécifications.",
            ["Tune_Back"] = "← Retour",
            ["Tune_ActiveTasks"] = "Tâches Actives",
            ["Tune_NewUpload"] = "+ Télécharger un fichier",
            ["Tune_NoActiveTasks"] = "Aucune tâche active",
            ["Tune_VehSpecs"] = "SPÉCIFICATIONS VÉHICULE",
            ["Tune_Producer"] = "CONSTRUCTEUR",
            ["Tune_Model"] = "MODÈLE",
            ["Tune_YearChassis"] = "ANNÉE / CHÂSSIS",
            ["Tune_BuildType"] = "FINITION / TYPE",
            ["Tune_EngSpecs"] = "SPÉCIFICATIONS MOTEUR",
            ["Tune_NameType"] = "CODE / TYPE",
            ["Tune_Displacement"] = "CYLINDRÉE",
            ["Tune_Output"] = "PUISSANCE (CH / KW)",
            ["Tune_Emission"] = "NORME POLLUTION",
            ["Tune_Transmission"] = "BOÎTE DE VITESSES",
            ["Tune_EcuSpecs"] = "SPÉCIFICATIONS CALCULATEUR",
            ["Tune_ProdBuild"] = "FABRICANT / MODÈLE",
            ["Tune_HwNr"] = "STAGE / RÉF HW",
            ["Tune_ProdNr"] = "NUMÉRO PIÈCE",
            ["Tune_SwVersion"] = "VERSION SOFTWARE",
            ["Tune_SwSize"] = "TAILLE SOFTWARE",
            ["Tune_AvailableTunes"] = "CARTOGRAPHIES & OPTIONS DISPONIBLES",
            ["Tune_OriginalFile"] = "Fichier Original",
            ["Tune_Save"] = "Enregistrer"
        }
    };

    public static string Get(string key)
    {
        if (Translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            return val;

        if (Translations["en"].TryGetValue(key, out var fallback))
            return fallback;

        return key;
    }

    private static string LoadSavedLanguage()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string saved = File.ReadAllText(SettingsFilePath).Trim();
                if (saved == "fr" || saved == "en")
                    return saved;
            }
        }
        catch { }
        return "en";
    }

    public static void SetLanguage(string langCode)
    {
        if (langCode != "en" && langCode != "fr")
            langCode = "en";

        if (CurrentLanguage != langCode)
        {
            CurrentLanguage = langCode;
            SaveLanguage(langCode);
            LanguageChanged?.Invoke();
        }
    }

    private static void SaveLanguage(string langCode)
    {
        try
        {
            string? dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(SettingsFilePath, langCode);
        }
        catch { }
    }
}
