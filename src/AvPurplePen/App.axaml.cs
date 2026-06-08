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
            // Set the app name for macOS menu bar (must happen before window creation)
            Name = "Purple Pen";
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDeveloperTools();
#endif
            RequestedThemeVariant = ThemeVariant.Light;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
                Controller controller = new Controller(mainWindowViewModel);

                MainWindow mainWindow = new MainWindow {
                    DataContext = mainWindowViewModel,
                };
                desktop.MainWindow = mainWindow;
                App.MainWindow = mainWindow;

                // Show initial screen once the main window is fully opened
                mainWindow.Opened += async (_, _) => { await ShowInitialScreenAsync(); };
            }

            base.OnFrameworkInitializationCompleted();

            ApplicationIdleService.Initialize();
        }

        /// <summary>
        /// Shows the initial/welcome screen. Loops if the user creates a new
        /// event but cancels the wizard. Exits if the user clicks Cancel.
        /// </summary>
        private static async Task ShowInitialScreenAsync()
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
                bool result = await dialog.ShowDialog<bool>(MainWindow!);

                if (!result) {
                    Environment.Exit(0);
                    return;
                }

                if (MainWindow?.DataContext is MainWindowViewModel mainVm && mainVm.Controller != null) {
                    var c = mainVm.Controller;

                    switch (vm.SelectedChoice) {
                        case InitialScreenChoice.NewEvent: {
                            var wizardVm = new NewEventWizardDialogViewModel();
                            bool wizardResult = await Services.DialogService.ShowDialogAsync(wizardVm);
                            if (wizardResult) {
                                bool success = await c.NewEvent(wizardVm.CreateEventInfo);
                                if (success) return;
                            }
                            continue;
                        }

                        case InitialScreenChoice.OpenExisting: {
                            var fileOpenVm = new FileOpenSingleViewModel {
                                FileFilters = MiscText.OpenFileDialog_PurplePenFilter,
                                InitialFileFilterIndex = 1
                            };
                            bool fileResult = await Services.DialogService.ShowDialogAsync(fileOpenVm);
                            if (fileResult && fileOpenVm.SelectedFile != null) {
                                bool success = await c.LoadNewFile(fileOpenVm.SelectedFile);
                                if (success) return;
                            }
                            continue;
                        }

                        case InitialScreenChoice.OpenLast:
                            await c.LoadNewFile(UserSettings.Current.LastLoadedFile);
                            return;

                        case InitialScreenChoice.OpenSample:
                            await c.LoadNewFile(SampleEventFileName());
                            return;
                    }
                }
                return;
            }
        }

        private static string SampleEventFileName()
        {
            string baseDir = AppContext.BaseDirectory;
            // In the .app bundle, the sample is copied to Resources/
            string resourcePath = Path.Combine(baseDir, "..", "Resources", "Sample Event.ppen");
            if (File.Exists(resourcePath))
                return Path.GetFullPath(resourcePath);
            // Fallback: look relative to source tree (dev builds)
            string devPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "doc", "userdocs", "Sample", "Sample Event.ppen"));
            if (File.Exists(devPath))
                return devPath;
            // Second fallback: TestFiles
            return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "TestFiles", "mapdisplay", "SampleEvent.ppen"));
        }
    }
}
