// NewEventWizardViewModel.cs
//
// Master ViewModel for the New Event Wizard. Manages page navigation and
// assembles a Controller.CreateEventInfo when the user finishes.
//
// Pages (in order, BitmapScale skipped for OCAD maps):
//   0 Title → 1 MapFile → [2 BitmapScale] → 3 PrintScale →
//   4 PaperSize → 5 Directory → 6 Standards → 7 Numbering → 8 Final
//
// Migrated from WinForms PurplePen/NewEventWizard.cs.

using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// Drives the New Event Wizard. The View binds CurrentPage to a ContentControl
    /// whose DataTemplates map each page ViewModel type to a UserControl view.
    /// </summary>
    public partial class NewEventWizardDialogViewModel : ViewModelBase
    {
        // All page ViewModels (instantiated once and reused on back-navigation).
        public NewEventTitlePageViewModel TitlePage { get; } = new();
        public NewEventMapFilePageViewModel MapFilePage { get; } = new();
        public NewEventBitmapScalePageViewModel BitmapScalePage { get; } = new();
        public NewEventPrintScalePageViewModel PrintScalePage { get; } = new();
        public NewEventPaperSizePageViewModel PaperSizePage { get; } = new();
        public NewEventDirectoryPageViewModel DirectoryPage { get; } = new();
        public NewEventStandardsPageViewModel StandardsPage { get; } = new();
        public NewEventNumberingPageViewModel NumberingPage { get; } = new();
        public NewEventFinalPageViewModel FinalPage { get; } = new();

        private int pageIndex = 0;

        /// <summary>The page ViewModel currently shown in the ContentControl.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanGoBack))]
        [NotifyPropertyChangedFor(nameof(CanGoNext))]
        [NotifyPropertyChangedFor(nameof(IsOnLastPage))]
        [NotifyPropertyChangedFor(nameof(NextButtonText))]
        private INewEventPageViewModel currentPage;

        /// <summary>Page title shown in the wizard header.</summary>
        [ObservableProperty]
        private string pageTitle = "";

        public bool CanGoBack => pageIndex > 0;
        public bool CanGoNext => CurrentPage?.CanProceed == true;
        public bool IsOnLastPage => pageIndex == GetPageList().Count - 1;
        public string NextButtonText => (IsOnLastPage ? MiscText.FinishButtonText : MiscText.NextButtonText).Replace("&", "");

        public NewEventWizardDialogViewModel()
        {
            currentPage = TitlePage;
            PageTitle = TitlePage.PageTitle;
            SubscribeToCurrentPage(null, TitlePage);
        }

        // Track when CurrentPage changes so we can re-subscribe to CanProceed changes.
        partial void OnCurrentPageChanged(INewEventPageViewModel oldValue, INewEventPageViewModel newValue)
        {
            SubscribeToCurrentPage(oldValue, newValue);
        }

        private void SubscribeToCurrentPage(INewEventPageViewModel? oldPage, INewEventPageViewModel newPage)
        {
            if (oldPage is System.ComponentModel.INotifyPropertyChanged oldNotify)
                oldNotify.PropertyChanged -= OnPagePropertyChanged;
            if (newPage is System.ComponentModel.INotifyPropertyChanged newNotify)
                newNotify.PropertyChanged += OnPagePropertyChanged;
        }

        private void OnPagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INewEventPageViewModel.CanProceed)) {
                OnPropertyChanged(nameof(CanGoNext));
                GoNextCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void GoNext()
        {
            List<INewEventPageViewModel> pages = GetPageList();

            if (pageIndex < pages.Count - 1) {
                int nextIndex = pageIndex + 1;
                PreparePageOnEntry(pages[nextIndex]);
                pageIndex = nextIndex;
                CurrentPage = pages[pageIndex];
                PageTitle = CurrentPage.PageTitle;
            }
            else {
                PrepareCreateEventInfo();
                IsComplete = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void GoBack()
        {
            if (pageIndex > 0) {
                List<INewEventPageViewModel> pages = GetPageList();
                pageIndex--;
                CurrentPage = pages[pageIndex];
                PageTitle = CurrentPage.PageTitle;
            }
        }

        /// <summary>Set to true when the user clicks Finish and all info is ready.</summary>
        public bool IsComplete { get; private set; }

        /// <summary>
        /// Advances to the next page. Returns true when the wizard is complete
        /// and the caller should close the dialog with a success result.
        /// </summary>
        public bool TryGoNext()
        {
            GoNextCommand.Execute(null);
            return IsComplete;
        }

        // Assembled on Finish; read by the command handler after ShowDialogAsync returns.
        public Controller.CreateEventInfo CreateEventInfo { get; private set; }

        // ---------- helpers ----------

        // Returns the ordered page list, skipping BitmapScale for OCAD maps.
        private List<INewEventPageViewModel> GetPageList()
        {
            List<INewEventPageViewModel> list = new() {
                TitlePage,
                MapFilePage,
            };

            if (MapFilePage.MapType == MapType.Bitmap || MapFilePage.MapType == MapType.PDF)
                list.Add(BitmapScalePage);

            list.Add(PrintScalePage);
            list.Add(PaperSizePage);
            list.Add(DirectoryPage);
            list.Add(StandardsPage);
            list.Add(NumberingPage);
            list.Add(FinalPage);
            return list;
        }

        // Called when a page is about to be shown (forward navigation).
        private void PreparePageOnEntry(INewEventPageViewModel page)
        {
            if (page is NewEventBitmapScalePageViewModel bitmapPage) {
                bitmapPage.IsPdf = MapFilePage.MapType == MapType.PDF;
                if (MapFilePage.Dpi > 0)
                    bitmapPage.DpiText = MapFilePage.Dpi.ToString();
                bitmapPage.ScaleText = MapFilePage.MapScale.ToString();
            }
            else if (page is NewEventPrintScalePageViewModel printPage) {
                float mapScale = MapFilePage.MapScale > 0 ? MapFilePage.MapScale : 15000f;
                printPage.InitializeScales(mapScale);
                printPage.PrintScaleText = mapScale.ToString();
            }
            else if (page is NewEventPaperSizePageViewModel paperPage) {
                float mapScale = MapFilePage.MapScale > 0 ? MapFilePage.MapScale : 15000f;
                float printScale = PrintScalePage.PrintScale > 0 ? PrintScalePage.PrintScale : mapScale;
                paperPage.AutoSelectPageSize(MapFilePage.MapBounds, printScale / mapScale, MapFilePage.MapType);
            }
            else if (page is NewEventFinalPageViewModel finalPage) {
                string dir = DirectoryPage.GetEventDirectory(MapFilePage.MapFileName);
                string fileName = TitlePage.GetEventFileName();
                finalPage.EventFilePath = Path.Combine(dir, fileName);
                finalPage.ErrorMessage = ValidateEventFile(finalPage.EventFilePath);
            }
        }

        // Validates that the event file can be created without overwriting.
        private static string ValidateEventFile(string eventFilePath)
        {
            string? directory = Path.GetDirectoryName(eventFilePath);
            if (string.IsNullOrEmpty(directory))
                return string.Format(MiscText.CannotCreateDirectory, "(unknown)");

            if (!Directory.Exists(directory)) {
                try {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e) {
                    return string.Format(MiscText.CannotCreateDirectory, directory) + "\n" + e.Message;
                }
            }

            if (File.Exists(eventFilePath))
                return string.Format(MiscText.FileAlreadyExists, Path.GetFileName(eventFilePath));

            try {
                File.WriteAllBytes(eventFilePath, new byte[] { 0 });
                File.Delete(eventFilePath);
            }
            catch (Exception e) {
                return string.Format(MiscText.CannotCreateFile, Path.GetFileName(eventFilePath)) + "\n" + e.Message;
            }

            return "";
        }

        // Assembles CreateEventInfo from all page VMs.
        private void PrepareCreateEventInfo()
        {
            string mapFile = MapFilePage.MapFileName;
            string dir = DirectoryPage.GetEventDirectory(mapFile);
            string fileName = TitlePage.GetEventFileName();

            Controller.CreateEventInfo info = new Controller.CreateEventInfo {
                title = TitlePage.EventTitle,
                eventFileName = Path.Combine(dir, fileName),
                mapType = MapFilePage.MapType,
                mapFileName = mapFile,
                scale = MapFilePage.MapScale,
                allControlsPrintScale = PrintScalePage.PrintScale,
                dpi = MapFilePage.MapType == MapType.Bitmap
                    ? BitmapScalePage.Dpi
                    : MapFilePage.Dpi,
                firstCode = NumberingPage.FirstCode,
                disallowInvertibleCodes = NumberingPage.DisallowInvertibleCodes,
                descriptionLangId = null,  // use default
                mapStandard = StandardsPage.MapStandard,
                descriptionStandard = StandardsPage.DescriptionStandard
            };

            // Purple color blending.
            if (info.mapType == MapType.OCAD && MapFilePage.LowerPurpleMapLayer != null) {
                info.blend = PurpleColorBlend.UpperLowerPurple;
                info.lowerPurpleLayer = MapFilePage.LowerPurpleMapLayer;
            }
            else {
                info.blend = PurpleColorBlend.Blend;
            }

            // Print area.
            PrintArea printArea = new PrintArea {
                autoPrintArea = true,
                restrictToPageSize = true,
                pageWidth = PaperSizePage.PageWidthHundreths,
                pageHeight = PaperSizePage.PageHeightHundreths,
                pageMargins = PaperSizePage.MarginHundreths,
                pageLandscape = PaperSizePage.Landscape
            };
            info.printArea = printArea;

            CreateEventInfo = info;
        }
    }
}
