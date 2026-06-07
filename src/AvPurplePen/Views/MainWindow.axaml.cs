// MainWindow.axaml.cs
//
// Code-behind for the main window. Handles UI events that need
// direct window interaction (like showing modal dialogs), which
// don't fit cleanly into the ViewModel layer.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvUtil;
using PurplePen;
using PurplePen.ViewModels;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AvPurplePen.Views
{
    /// <summary>
    /// The main application window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MousePointerShape _mousePointerShape = new MousePointerShape(PredefinedMousePointerShape.Arrow);
        private bool _closingConfirmed = false;

        // Has the MousePointerShape that should be used in the map viewer.
        public static readonly DirectProperty<MainWindow, MousePointerShape> MapMousePointerShapeProperty =
                AvaloniaProperty.RegisterDirect<MainWindow, MousePointerShape>(
                    nameof(MapMousePointerShape),
                    getter: o => o.MapMousePointerShape,
                    setter: (o, value) => o.MapMousePointerShape = value);

        /// <summary>
        /// Initializes the main window and its components.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ApplicationIdleService.ApplicationIdle += ApplicationIdle;
            Closing += MainWindow_Closing;
            DataContextChanged += MainWindow_DataContextChanged;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                SetupNativeMenu();
            }
        }

        /// <summary>
        /// Builds the macOS native menu bar. Hides the in-window menu and
        /// exports all menu items to the macOS system menu bar.
        /// </summary>
        private void SetupNativeMenu()
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm == null) {
                // DataContext not set yet — defer until DataContextChanged
                DataContextChanged += OnDataContextForNativeMenu;
                return;
            }
            BuildAndAttachNativeMenu(vm);
        }

        private void OnDataContextForNativeMenu(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm) {
                DataContextChanged -= OnDataContextForNativeMenu;
                BuildAndAttachNativeMenu(vm);
            }
        }

        private void BuildAndAttachNativeMenu(MainWindowViewModel vm)
        {
            var nativeMenu = new NativeMenu();

            // === File Menu ===
            var fileMenu = new NativeMenuItem { Header = UIText.MainFrame_fileMenu_Text };
            fileMenu.Menu = new NativeMenu();
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_newEventMenu_Text, vm.NewEventCommand));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_openMenu_Text, vm.FileOpenPurplePenFileCommand, new KeyGesture(Key.O, KeyModifiers.Meta)));
            fileMenu.Menu.Add(new NativeMenuItemSeparator());
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_saveMenu_Text, vm.SaveCommand, new KeyGesture(Key.S, KeyModifiers.Meta)));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_saveAsMenu_Text, vm.SaveAsCommand));
            fileMenu.Menu.Add(new NativeMenuItemSeparator());
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_createOcadFilesMenu_Text, vm.CreateOcadFilesCommand));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_createImageFilesMenu_Text, vm.CreateImageFilesCommand));

            // Create PDFs submenu
            var pdfMenu = new NativeMenuItem { Header = UIText.MainFrame_createPDFsMenu_Text };
            pdfMenu.Menu = new NativeMenu();
            pdfMenu.Menu.Add(CreateItem(UIText.MainFrame_createDescriptionPdfMenu_Text, vm.CreateDescriptionPdfCommand));
            pdfMenu.Menu.Add(CreateItem(UIText.MainFrame_createPunchcardPdfMenu_Text, vm.CreatePunchcardPdfCommand));
            pdfMenu.Menu.Add(CreateItem(UIText.MainFrame_createCoursePdfMenu_Text, vm.CreateCoursePdfCommand));
            fileMenu.Menu.Add(pdfMenu);

            // Route Review submenu
            var routeReviewMenu = new NativeMenuItem { Header = UIText.MainFrame_createRouteReviewFilesToolStripMenuItem_Text };
            routeReviewMenu.Menu = new NativeMenu();
            routeReviewMenu.Menu.Add(CreateItem(UIText.MainFrame_createRouteGadgetFilesMenu_Text, vm.CreateRouteGadgetFilesCommand));
            fileMenu.Menu.Add(routeReviewMenu);

            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_createXmlMenu_Text, vm.CreateXmlCommand));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_createGPXFileMenu_Text, vm.CreateGpxCommand));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_createKMLFileMenu_Text, vm.CreateKmlFilesCommand));
            fileMenu.Menu.Add(new NativeMenuItemSeparator());
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_printDescriptionsMenu_Text, vm.PrintDescriptionsCommand, new KeyGesture(Key.P, KeyModifiers.Meta)));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_printPunchCardsMenu_Text, vm.PrintPunchCardsCommand));
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_printCoursesMenu_Text, vm.PrintCoursesCommand));

            // Print Area submenu
            var printAreaMenu = new NativeMenuItem { Header = UIText.MainFrame_setPrintAreaMenu_Text };
            printAreaMenu.Menu = new NativeMenu();
            printAreaMenu.Menu.Add(CreateItem(UIText.MainFrame_printAreaThisPartMenu_Text, vm.SetPrintAreaThisPartCommand));
            printAreaMenu.Menu.Add(CreateItem(UIText.MainFrame_printAreaThisCourseMenu_Text, vm.SetPrintAreaThisCourseCommand));
            printAreaMenu.Menu.Add(CreateItem(UIText.MainFrame_printAreaAllCoursesMenu_Text, vm.SetPrintAreaAllCoursesCommand));
            fileMenu.Menu.Add(printAreaMenu);

            fileMenu.Menu.Add(new NativeMenuItemSeparator());
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_programLanguageMenu_Text, vm.ShowSwitchLanguageDialogCommand));
            fileMenu.Menu.Add(new NativeMenuItemSeparator());
            fileMenu.Menu.Add(CreateItem(UIText.MainFrame_exitMenu_Text, vm.ExitCommand));
            nativeMenu.Add(fileMenu);

            // === Edit Menu ===
            var editMenu = new NativeMenuItem { Header = UIText.MainFrame_editMenu_Text };
            editMenu.Menu = new NativeMenu();
            editMenu.Menu.Add(CreateItem(UIText.MainFrame_cancelMenu_Text, vm.CancelCommand));
            editMenu.Menu.Add(new NativeMenuItemSeparator());
            editMenu.Menu.Add(CreateItem(UIText.MainFrame_undoMenu_Text, vm.UndoCommand, new KeyGesture(Key.Z, KeyModifiers.Meta)));
            editMenu.Menu.Add(CreateItem(UIText.MainFrame_redoMenu_Text, vm.RedoCommand, new KeyGesture(Key.Y, KeyModifiers.Meta)));
            editMenu.Menu.Add(new NativeMenuItemSeparator());
            editMenu.Menu.Add(CreateItem(UIText.MainFrame_deleteMenu_Text, vm.DeleteSelectionCommand, new KeyGesture(Key.Delete)));
            nativeMenu.Add(editMenu);

            // === View Menu ===
            var viewMenu = new NativeMenuItem { Header = UIText.MainFrame_viewMenu_Text };
            viewMenu.Menu = new NativeMenu();
            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_entireCourseMenu_Text, vm.ViewEntireCourseCommand, new KeyGesture(Key.F2)));
            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_entireMapMenu_Text, vm.ViewEntireMapCommand, new KeyGesture(Key.F3)));

            // Zoom submenu
            var zoomMenu = new NativeMenuItem { Header = UIText.MainFrame_zoomMenu_Text };
            zoomMenu.Menu = new NativeMenu();
            foreach (var zoom in new[] { (0.5, UIText.MainFrame_zoom50Menu_Text, 0), (1.0, UIText.MainFrame_zoom100Menu_Text, 1),
                                       (1.5, UIText.MainFrame_zoom150Menu_Text, 2), (2.0, UIText.MainFrame_zoom200Menu_Text, 3),
                                       (3.0, UIText.MainFrame_zoom300Menu_Text, 4), (5.0, UIText.MainFrame_zoom500Menu_Text, 5),
                                       (10.0, UIText.MainFrame_zoom1000Menu_Text, 6) }) {
                zoomMenu.Menu.Add(CreateItem(zoom.Item2, vm.SetZoomCommand, null, zoom.Item1.ToString()));
            }
            viewMenu.Menu.Add(zoomMenu);

            viewMenu.Menu.Add(new NativeMenuItemSeparator());

            // Map Intensity submenu
            var intensityMenu = new NativeMenuItem { Header = UIText.MainFrame_mapIntensityMenu_Text };
            intensityMenu.Menu = new NativeMenu();
            foreach (var intensity in new[] { (0.2, UIText.MainFrame_veryLowIntensityMenu_Text),
                                              (0.4, UIText.MainFrame_lowIntensityMenu_Text),
                                              (0.6, UIText.MainFrame_mediumIntensityMenu_Text),
                                              (0.8, UIText.MainFrame_highIntensityMenu_Text),
                                              (1.0, UIText.MainFrame_fullIntensityMenu_Text) }) {
                intensityMenu.Menu.Add(CreateItem(intensity.Item2, vm.SetMapIntensityCommand, null, intensity.Item1.ToString()));
            }
            viewMenu.Menu.Add(intensityMenu);

            // Quality submenu
            var qualityMenu = new NativeMenuItem { Header = UIText.MainFrame_mapQualityMenu_Text };
            qualityMenu.Menu = new NativeMenu();
            qualityMenu.Menu.Add(CreateItem(UIText.MainFrame_normalQualityMenu_Text, vm.SetNormalQualityCommand));
            qualityMenu.Menu.Add(CreateItem(UIText.MainFrame_highQualityMenu_Text, vm.SetHighQualityCommand));
            viewMenu.Menu.Add(qualityMenu);

            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_showPrintAreaMenu_Text, vm.ToggleShowPrintAreaCommand));
            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_showPopupsMenu_Text, vm.ToggleShowPopupsCommand));
            viewMenu.Menu.Add(new NativeMenuItemSeparator());
            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_allControlsMenu_Text, vm.ToggleAllControlsCommand, new KeyGesture(Key.F4)));
            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_otherCoursesMenu_Text, vm.ShowOtherCoursesCommand, new KeyGesture(Key.F5)));
            viewMenu.Menu.Add(CreateItem(UIText.MainFrame_clearOtherCoursesMenu_Text, vm.ClearOtherCoursesCommand, new KeyGesture(Key.F5, KeyModifiers.Shift)));
            nativeMenu.Add(viewMenu);

            // === Add Menu ===
            var addMenu = new NativeMenuItem { Header = UIText.MainFrame_addMenu_Text };
            addMenu.Menu = new NativeMenu();
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addStartMenu_Text, vm.AddStartCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addControlMenu_Text, vm.AddControlCommand, new KeyGesture(Key.A, KeyModifiers.Meta)));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addFinishMenu_Text, vm.AddFinishCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addDescriptionsMenu_Text, vm.AddDescriptionsCommand));

            // Map Exchange submenu
            var mapExchangeMenu = new NativeMenuItem { Header = UIText.MainFrame_addMapExchangeMenu_Text };
            mapExchangeMenu.Menu = new NativeMenu();
            mapExchangeMenu.Menu.Add(CreateItem(UIText.MainFrame_addMapFlipMenuItem_Text, vm.AddMapFlipControlCommand));
            mapExchangeMenu.Menu.Add(CreateItem(UIText.MainFrame_mapExchangeControlMenuItem_Text, vm.AddMapExchangeControlCommand));
            mapExchangeMenu.Menu.Add(CreateItem(UIText.MainFrame_mapExchangeSeparateMenuItem_Text, vm.AddMapExchangeSeparateCommand));
            addMenu.Menu.Add(mapExchangeMenu);

            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addVariationMenu_Text, vm.AddVariationCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addTextLineMenu_Text, vm.AddTextLineCommand));
            addMenu.Menu.Add(new NativeMenuItemSeparator());
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addMapIssueMenu_Text, vm.AddMapIssueCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addMandatoryCrossingMenu_Text, vm.AddMandatoryCrossingCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addOptCrossingMenu_Text, vm.AddOptCrossingCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addOutOfBoundsMenu_Text, vm.AddOutOfBoundsCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addDangerousMenu_Text, vm.AddDangerousCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addConstructionMenu_Text, vm.AddConstructionCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addWaterMenu_Text, vm.AddWaterCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addFirstAidMenu_Text, vm.AddFirstAidCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addForbiddenMenu_Text, vm.AddForbiddenCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addBoundaryMenu_Text, vm.AddBoundaryCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addRegMarkMenu_Text, vm.AddRegMarkCommand));
            addMenu.Menu.Add(new NativeMenuItemSeparator());
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_whiteOutMenu_Text, vm.AddWhiteOutCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addTextMenu_Text, vm.AddTextCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addImageMenu_Text, vm.AddImageCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addLineMenu_Text, vm.AddLineCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addRectangleMenu_Text, vm.AddRectangleCommand));
            addMenu.Menu.Add(CreateItem(UIText.MainFrame_addEllipseMenu_Text, vm.AddEllipseCommand));
            nativeMenu.Add(addMenu);

            // === Event Menu ===
            var eventMenu = new NativeMenuItem { Header = UIText.MainFrame_eventMenu_Text };
            eventMenu.Menu = new NativeMenu();
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_changeMapFileMenu_Text, vm.ChangeMapFileCommand));
            eventMenu.Menu.Add(new NativeMenuItemSeparator());
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_changeCodesMenu_Text, vm.ChangeCodesCommand));
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_autoNumberingMenu_Text, vm.AutoNumberingCommand));
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_removeUnusedControlsMenu_Text, vm.RemoveUnusedControlsCommand));
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_moveAllControlsMenu_Text, vm.MoveAllControlsCommand));
            eventMenu.Menu.Add(new NativeMenuItemSeparator());
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_punchPatternsMenu_Text, vm.PunchPatternsCommand));
            eventMenu.Menu.Add(new NativeMenuItemSeparator());

            // IOF Standards submenu
            var iofMenu = new NativeMenuItem { Header = UIText.MainFrame_iOFStandardsToolStripMenuItem_Text };
            iofMenu.Menu = new NativeMenu();
            iofMenu.Menu.Add(CreateItem(UIText.MainFrame_descriptionStd2004Menu_Text, vm.SetDescriptionStd2004Command));
            iofMenu.Menu.Add(CreateItem(UIText.MainFrame_descriptionStd2018Menu_Text, vm.SetDescriptionStd2018Command));
            iofMenu.Menu.Add(new NativeMenuItemSeparator());
            iofMenu.Menu.Add(CreateItem(UIText.MainFrame_mapStd2000Menu_Text, vm.SetMapStd2000Command));
            iofMenu.Menu.Add(CreateItem(UIText.MainFrame_mapStd2017Menu_Text, vm.SetMapStd2017Command));
            iofMenu.Menu.Add(CreateItem(UIText.MainFrame_mapStdSpr2019Menu_Text, vm.SetMapStdSpr2019Command));
            eventMenu.Menu.Add(iofMenu);

            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_customizeDescriptionsMenu_Text, vm.CustomizeDescriptionsCommand));
            eventMenu.Menu.Add(CreateItem(UIText.MainFrame_customizeCourseAppearanceMenu_Text, vm.CustomizeCourseAppearanceCommand));
            nativeMenu.Add(eventMenu);

            // === Course Menu ===
            var courseMenu = new NativeMenuItem { Header = UIText.MainFrame_courseMenu_Text };
            courseMenu.Menu = new NativeMenu();
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_addCourseMenu_Text, vm.ShowAddCourseDialogCommand));
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_deleteCourseMenu_Text, vm.DeleteCourseCommand));
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_duplicateCourseMenu_Text, vm.DuplicateCourseCommand));
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_propertiesMenu_Text, vm.ShowCoursePropertiesCommand));
            courseMenu.Menu.Add(new NativeMenuItemSeparator());
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_courseOrderMenu_Text, vm.ShowCourseOrderCommand));
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_courseLoadMenu_Text, vm.ShowCourseLoadCommand));
            courseMenu.Menu.Add(new NativeMenuItemSeparator());
            courseMenu.Menu.Add(CreateItem(UIText.MainFrame_courseVariationReportMenu_Text, vm.ShowCourseVariationReportCommand));
            nativeMenu.Add(courseMenu);

            // === Item Menu ===
            var itemMenu = new NativeMenuItem { Header = UIText.MainFrame_itemMenu_Text };
            itemMenu.Menu = new NativeMenu();
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_deleteItemMenu_Text, vm.DeleteSelectionCommand, new KeyGesture(Key.Delete)));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_deleteForkMenu_Text, vm.DeleteForkCommand));
            itemMenu.Menu.Add(new NativeMenuItemSeparator());
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_addBendMenu_Text, vm.AddBendCommand, new KeyGesture(Key.B, KeyModifiers.Meta)));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_removeBendMenu_Text, vm.RemoveBendCommand, new KeyGesture(Key.B, KeyModifiers.Meta | KeyModifiers.Shift)));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_addGapMenu_Text, vm.AddGapCommand, new KeyGesture(Key.G, KeyModifiers.Meta)));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_removeGapMenu_Text, vm.RemoveGapCommand, new KeyGesture(Key.G, KeyModifiers.Meta | KeyModifiers.Shift)));
            itemMenu.Menu.Add(new NativeMenuItemSeparator());
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_changeTextMenu_Text, vm.ChangeTextCommand));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_changeLineAppearanceMenu_Text, vm.ChangeLineAppearanceCommand));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_rotateMenu_Text, vm.RotateCommand));
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_stretchMenu_Text, vm.StretchCommand));

            // Leg Flagging submenu
            var flaggingMenu = new NativeMenuItem { Header = UIText.MainFrame_legFlaggingMenu_Text };
            flaggingMenu.Menu = new NativeMenu();
            flaggingMenu.Menu.Add(CreateItem(UIText.MainFrame_noFlaggingMenu_Text, vm.SetNoFlaggingCommand));
            flaggingMenu.Menu.Add(CreateItem(UIText.MainFrame_entireFlaggingMenu_Text, vm.SetEntireFlaggingCommand));
            flaggingMenu.Menu.Add(CreateItem(UIText.MainFrame_beginFlaggingMenu_Text, vm.SetBeginFlaggingCommand));
            flaggingMenu.Menu.Add(CreateItem(UIText.MainFrame_endFlaggingMenu_Text, vm.SetEndFlaggingCommand));
            itemMenu.Menu.Add(flaggingMenu);

            itemMenu.Menu.Add(new NativeMenuItemSeparator());
            itemMenu.Menu.Add(CreateItem(UIText.MainFrame_changeDisplayedCoursesMenu_Text, vm.ChangeDisplayedCoursesCommand));
            nativeMenu.Add(itemMenu);

            // === Reports Menu ===
            var reportMenu = new NativeMenuItem { Header = UIText.MainFrame_reportMenu_Text };
            reportMenu.Menu = new NativeMenu();
            reportMenu.Menu.Add(CreateItem(UIText.MainFrame_courseSummaryMenu_Text, vm.ShowCourseSummaryCommand));
            reportMenu.Menu.Add(CreateItem(UIText.MainFrame_eventAuditMenu_Text, vm.ShowEventAuditCommand));
            reportMenu.Menu.Add(CreateItem(UIText.MainFrame_legLengthsMenu_Text, vm.ShowLegLengthsCommand));
            reportMenu.Menu.Add(CreateItem(UIText.MainFrame_controlCrossrefMenu_Text, vm.ShowControlCrossrefCommand));
            reportMenu.Menu.Add(CreateItem(UIText.MainFrame_controlAndLegLoadMenu_Text, vm.ShowControlAndLegLoadCommand));
            nativeMenu.Add(reportMenu);

            // === Help Menu ===
            var helpMenu = new NativeMenuItem { Header = UIText.MainFrame_helpMenu_Text };
            helpMenu.Menu = new NativeMenu();
            helpMenu.Menu.Add(CreateItem(UIText.MainFrame_helpContentsMenu_Text, vm.HelpContentsCommand, new KeyGesture(Key.F1)));
            helpMenu.Menu.Add(CreateItem(UIText.MainFrame_helpTranslatedMenu_Text, vm.HelpTranslatedCommand));
            helpMenu.Menu.Add(new NativeMenuItemSeparator());
            helpMenu.Menu.Add(CreateItem(UIText.MainFrame_mainWebSiteToolMenu_Text, vm.OpenMainWebSiteCommand));
            helpMenu.Menu.Add(CreateItem(UIText.MainFrame_supportWebSiteMenu_Text, vm.OpenSupportWebSiteCommand));
            helpMenu.Menu.Add(CreateItem(UIText.MainFrame_donateWebSiteMenu_Text, vm.OpenDonateWebSiteCommand));
            helpMenu.Menu.Add(new NativeMenuItemSeparator());
            helpMenu.Menu.Add(CreateItem(UIText.MainFrame_aboutMenu_Text, vm.ShowAboutDialogCommand));

#if DEBUG
            // Debug submenu — only in debug builds
            var debugMenu = new NativeMenuItem { Header = UIText.MainFrame_debugMenu_Text };
            debugMenu.Menu = new NativeMenu();
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_symbolBrowserMenu_Text, vm.ShowSymbolBrowserCommand));
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_descriptionBrowserMenu_Text, vm.ShowDescriptionBrowserCommand));
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_controlTesterMenu_Text, vm.ShowControlTesterCommand));
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_mapTesterMenu_Text, vm.ShowMapTesterCommand));
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_courseSelectorTesterMenu_Text, vm.ShowCourseSelectorTesterCommand));
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_dotGridTesterToolStripMenuItem_Text, vm.ShowDotGridTesterCommand));
            debugMenu.Menu.Add(CreateItem(UIText.MainFrame_dumpOCADFileMenu_Text, vm.DumpOcadFileCommand));
            helpMenu.Menu.Add(debugMenu);
#endif

            nativeMenu.Add(helpMenu);

            // Attach to window — this exports to the macOS native menu bar
            NativeMenu.SetMenu(this, nativeMenu);

            // Hide the in-window menu on macOS
            mainMenu.IsVisible = false;
        }

        /// <summary>
        /// Creates a NativeMenuItem with a header, command, optional gesture, and optional command parameter.
        /// </summary>
        private static NativeMenuItem CreateItem(string header, System.Windows.Input.ICommand? command,
            KeyGesture? gesture = null, string? commandParameter = null)
        {
            var item = new NativeMenuItem { Header = header };
            if (command != null) {
                item.Command = command;
                if (commandParameter != null)
                    item.CommandParameter = commandParameter;
            }
            if (gesture != null)
                item.Gesture = gesture;
            return item;
        }

        private void MainWindow_DataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm) {
                vm.CloseRequested += (_, _) => Close();
                vm.ShowRectangleCallback = bounds => mapViewer.ShowRectangle(bounds);
            }
        }

        // Intercepts window close (both ✕ button and File→Exit).
        // Avalonia's Closing event is synchronous, so we cancel immediately,
        // do the async "save changes?" check, then re-close if approved.
        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (_closingConfirmed) return;
            e.Cancel = true;

            if (DataContext is MainWindowViewModel vm && await vm.TryCloseAsync()) {
                _closingConfirmed = true;
                Close();
            }
        }

        public MousePointerShape MapMousePointerShape {
            get => _mousePointerShape;
            set {
                _mousePointerShape = value;
                mapViewer.Cursor = Cursors.CursorFromMousePointerShape(value);
            }
        }

        // Mouse activity in the main map viewer.
        private async void MapViewer_MouseActivity(object? sender, MapViewer.FancyMouseEventArgs e)
        {
            MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;
            if (vm == null)
                return;

            // Only left and right buttons have meaning (except for move)
            if (e.Button != MouseButton.Left && e.Button != MouseButton.Right && e.FancyAction != MapViewer.FancyMouseAction.Move)
                return;

            bool isRightButton = (e.Button == MouseButton.Right);
            PointF location = Conv.ToPointF(e.WorldLocation);
            PointF locationStart = Conv.ToPointF(e.WorldDragStart);
            float pixelSize = mapViewer.PixelSize;
            DragAction dragAction = DragAction.None;
            
            switch (e.FancyAction) {
            case MapViewer.FancyMouseAction.Move:
#if PORTING
                // Do we need to deal with leave here to report outside the viewport?
#endif
                vm.MapViewerMouseMove(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Down:
                if (isRightButton)
                    dragAction = vm.MapViewerRightButtonDown(location, pixelSize);
                else
                    dragAction = vm.MapViewerLeftButtonDown(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Drag:
                if (isRightButton)
                    vm.MapViewerRightButtonDrag(location, locationStart, pixelSize);
                else
                    vm.MapViewerLeftButtonDrag(location, locationStart, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Up:
                if (isRightButton) 
                    vm.MapViewerRightButtonUp(location, pixelSize);
                else
                    vm.MapViewerLeftButtonUp(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.DragEnd:
                if (isRightButton)
                    await vm.MapViewerRightButtonEndDrag(location, locationStart, pixelSize);
                else
                    await vm.MapViewerLeftButtonEndDrag(location, locationStart, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Click:
                if (isRightButton)
                    await vm.MapViewerRightButtonClick(location, pixelSize);
                else
                    await vm.MapViewerLeftButtonClick(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.DragCancel:
                if (isRightButton)
                    vm.MapViewerRightButtonCancelDrag();
                else
                    vm.MapViewerLeftButtonCancelDrag();
                break;

            case MapViewer.FancyMouseAction.Hover:
#if !PORTING
                // handle hover
#endif
                break;

            default:
                break;
            }

            switch (dragAction) {
            case DragAction.None:
                e.MouseDownResult = MapViewer.MouseDownResult.None; break;
            case DragAction.SuppressClick:
                e.MouseDownResult = MapViewer.MouseDownResult.SuppressClick; break;
            case DragAction.MapDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.ImmediatePan;  break;
            case DragAction.ImmediateDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.ImmediateDrag; break;
            case DragAction.DelayedDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.DelayedDrag; break;
            default:
                break;
            }
        }


        // This is called when the application becomes idle after processing input. We can use this to update
        // the UI in response to changes that may have occurred.
        private void ApplicationIdle(object? sender, System.EventArgs e)
        {
            if (this.IsVisible) {
                // The application is idle. If the application state has changed, update the
                // user interface to match.
                if (this.DataContext is MainWindowViewModel viewModel) {
                    viewModel.UpdateStateOnIdle();
                }
            }
        }
    }
}
