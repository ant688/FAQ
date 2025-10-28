using CommunityToolkit.Mvvm.Messaging;
using Lepo.i18n;
using PDFWand.Helpers;
using PDFWand.Messenger;
using PDFWand.Models;
using PDFWand.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PDFWand.ViewModels.Pages
{
    public partial class SettingsViewModel(ILocalSettingsService localSettingsService,
      IContentDialogService contentDialogService,
      ISnackbarService snackbarService, IMessenger messenger, ILocalizationProvider localizationProvider) : ObservableObject, INavigationAware
    {
        private const string SystemLanguageCode = "system";
        private bool _isInitialized = false;
        private bool _isApplyingLanguage = false;
        private readonly ILocalSettingsService _localSettingsService = localSettingsService;
        private readonly IContentDialogService _contentDialogService = contentDialogService;
        private readonly ISnackbarService _snackbarService = snackbarService;
        private readonly IMessenger _messenger = messenger;
        private readonly ILocalizationProvider _localizationProvider = localizationProvider;
        private LocalizationSet? _localizationSet;

        [ObservableProperty]
        private string _appVersion = String.Empty;


        [ObservableProperty]
        private LocalLanguage? _selectedLanguage;
        partial void OnSelectedLanguageChanged(LocalLanguage? value)
        {
            if (!_isInitialized || _isApplyingLanguage || value is null || string.IsNullOrWhiteSpace(value.Code))
            {
                return;
            }

            var languageCode = value.Code!;
            _ = _localSettingsService.SaveSettingAsync("lang", languageCode);

            CultureInfo culture;

            if (string.Equals(languageCode, SystemLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    culture = new CultureInfo(CultureInfo.CurrentUICulture.Name);
                }
                catch
                {
                    culture = new CultureInfo("en-US");
                }
            }
            else
            {
                try
                {
                    culture = new CultureInfo(languageCode);
                }
                catch
                {
                    return;
                }
            }

            var localizationSet = _localizationProvider.GetLocalizationSet(culture.Name);

            if (localizationSet is null)
            {
                culture = new CultureInfo("en-US");
                localizationSet = _localizationProvider.GetLocalizationSet(culture.Name);
            }

            if (localizationSet is null)
            {
                return;
            }

            if (string.Equals(_localizationProvider.GetCulture().Name, culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _isApplyingLanguage = true;

            try
            {
                _localizationProvider.SetCulture(culture);
                _localizationSet = localizationSet;

                var selectedThemeCode = SelectedTheme.Code;

                UpdateLanguageList(languageCode);
                UpdateThemeList(selectedThemeCode);

                _snackbarService.Show(_localizationSet!["LanguageChangedToastTitle"]!, _localizationSet!["LanguageChangedToastContent"]!, ControlAppearance.Success, StencilIconHelper.GetIcon("commercial-32"), TimeSpan.FromSeconds(5));

                _messenger.Send(new LanguageChangedMessage(culture));
            }
            finally
            {
                _isApplyingLanguage = false;
            }
        }


        [ObservableProperty]
        private ObservableCollection<LocalLanguage> _languageList = [];

        [ObservableProperty]
        private ObservableCollection<string>? _listExceptionBodyCanon = [];

        [ObservableProperty]
        private ObservableCollection<LocalTheme>? _listLocalTheme = [];

        [ObservableProperty]
        private LocalTheme _selectedTheme = new() { Code = "Unknown", DisplayName = "System" };
        partial void OnSelectedThemeChanged(LocalTheme value)
        {
            _ = _localSettingsService.SaveSettingAsync("Theme", value.Code);
            _messenger.Send(new ThemeChangeMessage(value));
            Debug.Print("Message sent !");
            switch (value.Code)
            {
                case "Light":
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    break;
                case "Dark":
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    break;
                case "HighContrast":
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
                    break;
                default:
                    break;
            }
        }

        [ObservableProperty]
        private bool _isPreviewAllPage;
        partial void OnIsPreviewAllPageChanged(bool value)
        {
           _= _localSettingsService.SaveSettingAsync("PreviewAllPage", value);
        }


        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                await InitializeViewModel();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task InitializeViewModel()
        {

            AppVersion = $"Version: {GetAssemblyVersion()}";
            IsPreviewAllPage = await _localSettingsService.ReadSettingAsync<bool>("PreviewAllPage");
            string _theme = await _localSettingsService.ReadSettingAsync<string>("Theme");
            string languageSetting = await _localSettingsService.ReadSettingAsync<string>("lang");
            var defaultCulture = new CultureInfo("en-US");
            var culture = defaultCulture;
            string selectedLanguageCode = languageSetting;

            if (string.IsNullOrWhiteSpace(languageSetting))
            {
                selectedLanguageCode = SystemLanguageCode;
            }

            if (string.Equals(selectedLanguageCode, SystemLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    culture = new CultureInfo(CultureInfo.CurrentUICulture.Name);
                }
                catch
                {
                    culture = defaultCulture;
                }
            }

            if (!string.Equals(selectedLanguageCode, SystemLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    culture = new CultureInfo(selectedLanguageCode);
                }
                catch
                {
                    culture = defaultCulture;
                    selectedLanguageCode = defaultCulture.Name;
                }
            }

            _localizationSet = _localizationProvider.GetLocalizationSet(culture.Name)
                ?? _localizationProvider.GetLocalizationSet(defaultCulture.Name);

            if (_localizationSet is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(languageSetting) || (!string.Equals(languageSetting, selectedLanguageCode, StringComparison.OrdinalIgnoreCase) && !string.Equals(selectedLanguageCode, SystemLanguageCode, StringComparison.OrdinalIgnoreCase)))
            {
                await _localSettingsService.SaveSettingAsync("lang", selectedLanguageCode);
            }
            else if (string.Equals(selectedLanguageCode, SystemLanguageCode, StringComparison.OrdinalIgnoreCase) && !string.Equals(languageSetting, SystemLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                await _localSettingsService.SaveSettingAsync("lang", SystemLanguageCode);
            }

            UpdateLanguageList(selectedLanguageCode);

            UpdateThemeList(_theme);

            //GetListException();
            _isInitialized = true;
        }

        private void UpdateLanguageList(string? selectedCode)
        {
            if (_localizationSet is null)
            {
                LanguageList = [];
                return;
            }

            var languages = new ObservableCollection<LocalLanguage>
            {
                new()
                {
                    DisplayName = _localizationSet!["SystemLanguage"],
                    Code = SystemLanguageCode,
                },
                new()
                {
                    DisplayName = _localizationSet!["English"],
                    Code = "en-US",
                },
                new()
                {
                    DisplayName = _localizationSet!["Vietnamese"],
                    Code = "vi-VN",
                },
                new()
                {
                    DisplayName = _localizationSet!["Japanese"],
                    Code = "ja-JP",
                },
                new()
                {
                    DisplayName = _localizationSet!["ChineseSimplified"],
                    Code = "zh-CN",
                },
                new()
                {
                    DisplayName = _localizationSet!["ChineseTraditional"],
                    Code = "zh-TW",
                },
                new()
                {
                    DisplayName = _localizationSet!["French"],
                    Code = "fr-FR",
                }
            };

            LanguageList = languages;

            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                selectedCode = SystemLanguageCode;
            }

            SelectedLanguage = languages.FirstOrDefault(x => string.Equals(x.Code, selectedCode, StringComparison.OrdinalIgnoreCase))
                ?? languages.FirstOrDefault();
        }

        private void UpdateThemeList(string? selectedThemeCode)
        {
            if (_localizationSet is null)
            {
                ListLocalTheme = [];
                return;
            }

            var themes = new ObservableCollection<LocalTheme>
            {
                new()
                {
                    Code = "Light",
                    DisplayName = _localizationSet!["Light"]
                },
                new()
                {
                    Code = "Dark",
                    DisplayName = _localizationSet!["Dark"]
                },
                new()
                {
                    Code = "HighContrast",
                    DisplayName = _localizationSet!["HighContrast"]
                },
                new()
                {
                    Code = "Unknown",
                    DisplayName = _localizationSet!["System"]
                }
            };

            ListLocalTheme = themes;

            if (string.IsNullOrWhiteSpace(selectedThemeCode))
            {
                selectedThemeCode = SelectedTheme.Code;
            }

            SelectedTheme = themes.FirstOrDefault(x => x.Code == selectedThemeCode)
                ?? themes.First();
        }

        private async void RestartApp()
        {
            var dlg = new ContentDialog
            {
                Title = "",
                Content = "",
                PrimaryButtonText = "Restart",
                SecondaryButtonText = "Cancel",
            };
            var xn = await _contentDialogService.ShowAsync(dlg, CancellationToken.None);
            if (xn == ContentDialogResult.Primary)
            {
                //System.Windows.Application.Current.Shutdown();
                //System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);

                // Shut down the current instance
                System.Windows.Application.Current.Shutdown();

                // Get the path of the current executable
                string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                // Start a new instance of the application
                Process.Start(executablePath);

               
            }
        }

        private static string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

    }
}
