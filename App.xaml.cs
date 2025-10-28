using CommunityToolkit.Mvvm.Messaging;
using Lepo.i18n;
using Lepo.i18n.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PDFWand.Resources;
using PDFWand.Services;
using PDFWand.ViewModels;
using PDFWand.ViewModels.Pages;
using PDFWand.ViewModels.Windows;
using PDFWand.Views.Pages;
using PDFWand.Views.Windows;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace PDFWand
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static Mutex? _appMutex;
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<ILogInputService, LogInputService>();
                services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);



                //services.AddSingleton<ILocalizationProvider>();
                services.AddStringLocalizer(b =>
                {
                    b.FromResource<Translations>(new("vi-VN"));
                    b.FromResource<Translations>(new("en-US"));
                    b.FromResource<Translations>(new("ja-JP"));
                    b.FromResource<Translations>(new("zh-CN"));
                    b.FromResource<Translations>(new("zh-TW"));
                    b.FromResource<Translations>(new("fr-FR"));
                });


                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();

                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<ExtractionViewModel>();
                services.AddSingleton<ExtractionPage>();
                services.AddSingleton<PagingViewModel>();
                services.AddSingleton<PagingPage>();
                services.AddSingleton<MergePage>();
                services.AddSingleton<MergeViewModel>();
                services.AddSingleton<ProtectPage>();
                services.AddSingleton<ProtectViewModel>();
                services.AddSingleton<WatermarkPage>();
                services.AddSingleton<WatermarkViewModel>();

            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            const string appName = "PDFWand";
            _appMutex = new Mutex(true, appName, out bool isNewInstance);

            if (!isNewInstance)
            {
                Environment.Exit(0);
            }
            await _host.StartAsync();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            _appMutex?.ReleaseMutex();
            _appMutex?.Dispose();
            await _host.StopAsync();

            _host.Dispose();
        }

        public App()
        {
            var setting = _host.Services.GetRequiredService<ILocalSettingsService>();
            var lang = setting.ReadSettingAsync<string>("lang").Result;

            if (string.IsNullOrWhiteSpace(lang))
            {
                lang = "en-US";
                _ = setting.SaveSettingAsync("lang", lang);
            }
            ILocalizationProvider provider = _host.Services.GetRequiredService<ILocalizationProvider>();
            var cul = new CultureInfo("en-US");
            try
            {
                cul = new CultureInfo(lang);
            }
            catch { }
            provider.SetCulture(cul);
        }


        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
