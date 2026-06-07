using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvPurplePen.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    public partial class App : Application
    {
        /// <summary>
        /// The main application window. Set during initialization and used by
        /// the IDialogService factory to create modal dialogs.
        /// </summary>
        public static Window? MainWindow { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDeveloperTools();
#endif
            RequestedThemeVariant = ThemeVariant.Light;
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
                Controller controller = new Controller(mainWindowViewModel);

                MainWindow mainWindow = new MainWindow {
                    DataContext = mainWindowViewModel,
                };
                desktop.MainWindow = mainWindow;
                App.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();

            ApplicationIdleService.Initialize();

            // Show the initial/welcome screen after the main window is set up
            await ShowInitialScreenIfNeeded();
        }

        /// <summary>
        /// Shows the initial/welcome screen if no event is loaded (first launch).
        /// Loops if the user creates a new event but cancels the wizard.
        /// </summary>
        private async Task ShowInitialScreenIfNeeded()
        {
            while (true) {
                var vm = new InitialScreenViewModel {
                    CanOpenLast = File.Exists(UserSettings.Current.LastLoadedFile),
                    OpenLastText = File.Exists(UserSettings.Current.LastLoadedFile)
                        ? string.Format(MiscText.OpenLastEvent, Path.GetFileNameWithoutExtension(UserSettings.Current.LastLoadedFile))
                        : "",
                    CanOpenSample = File.Exists(SampleEventFileName())
                };

                var dialog = new InitialScreenDialog { DataContext = vm };
                bool result = await dialog.ShowDialog<bool>(App.MainWindow!);

                if (!result) {
                    // User clicked Cancel — exit the app
                    Environment.Exit(0);
                    return;
                }

                if (App.MainWindow?.DataContext is MainWindowViewModel mainVm && mainVm.Controller != null) {
                    var controller = mainVm.Controller;

                    switch (vm.SelectedChoice) {
                        case InitialScreenChoice.NewEvent: {
                            var wizardVm = new NewEventWizardDialogViewModel();
                            bool wizardResult = await Services.DialogService.ShowDialogAsync(wizardVm);
                            if (wizardResult) {
                                bool success = await controller.NewEvent(wizardVm.CreateEventInfo);
                                if (success) return;
                            }
                            continue; // Cancelled or failed — loop back
                        }

                        case InitialScreenChoice.OpenExisting: {
                            var fileOpenVm = new FileOpenSingleViewModel {
                                FileFilters = MiscText.OpenFileDialog_PurplePenFilter,
                                InitialFileFilterIndex = 1
                            };
                            bool fileResult = await Services.DialogService.ShowDialogAsync(fileOpenVm);
                            if (fileResult && fileOpenVm.SelectedFile != null) {
                                bool success = await controller.LoadNewFile(fileOpenVm.SelectedFile);
                                if (success) return;
                            }
                            continue;
                        }

                        case InitialScreenChoice.OpenLast:
                            await controller.LoadNewFile(UserSettings.Current.LastLoadedFile);
                            return;

                        case InitialScreenChoice.OpenSample:
                            await controller.LoadNewFile(SampleEventFileName());
                            return;
                    }
                }
                return;
            }
        }

        /// <summary>
        /// Returns the path to the sample event file bundled with the application.
        /// </summary>
        private static string SampleEventFileName()
        {
            string baseDir = AppContext.BaseDirectory;
            // Walk up from the bin output directory to find TestFiles
            return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "TestFiles", "SampleEvent2.ppen"));
        }
    }
}
