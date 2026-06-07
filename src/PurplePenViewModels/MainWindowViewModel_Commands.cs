// These are the implementations of commands for the menu and toolbar
// in the main windows.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;

namespace PurplePen.ViewModels
{
    public partial class MainWindowViewModel
    {
        // Persisted across invocations of each export dialog.
        private CoursePdfSettings? coursePdfSettings;
        private OcadCreationSettings? ocadCreationSettingsPrevious;
        private BitmapCreationSettings? bitmapCreationSettingsPrevious;

        // Update the state of menu items and toolbar buttons, which are
        // typically observable properties.
        private void UpdateMenusToolbarButtons()
        {
            if (controller == null) { return; }

            // Update enabled status.
            CanAddBend = (controller.CanAddBend() == CommandStatus.Enabled);

            // Update checked status of Zoom.
            Zoom50Checked = UpdateZoomChecked(0.5F);
            Zoom100Checked = UpdateZoomChecked(1.0F);
            Zoom150Checked = UpdateZoomChecked(1.5F);
            Zoom200Checked = UpdateZoomChecked(2.0F);
            Zoom300Checked = UpdateZoomChecked(3.0F);
            Zoom500Checked = UpdateZoomChecked(5.0F);
            Zoom1000Checked = UpdateZoomChecked(10.0F);

            // Update checked status of Intensity.
            IntensityVeryLowChecked = UpdateIntensityChecked(0.2F);
            IntensityLowChecked = UpdateIntensityChecked(0.4F);
            IntensityMediumChecked = UpdateIntensityChecked(0.6F);
            IntensityHighChecked = UpdateIntensityChecked(0.8F);
            IntensityFullChecked = UpdateIntensityChecked(1.0F);

            // Update checked status of Quality.
            HighQualityMapDisplay = MapDisplay?.AntiAlias ?? true;

            // Update checked status of Show All Controls.
            ViewAllControlsChecked = controller.ShowAllControls;

        }

        // Determine if the give zoom label (e.g. "100%") should be checked based on the current zoom factor.
        bool UpdateZoomChecked(float zoomLabel)
        {
            return Math.Abs(MapZoomFactor/zoomLabel - 1.0F) < 0.05F;
        }

        // Determine if the give zoom label (e.g. "100%") should be checked based on the current zoom factor.
        bool UpdateIntensityChecked(float intensityLabel)
        {
            if (MapDisplay == null) { return false; }

            return Math.Abs(MapDisplay.MapIntensity / intensityLabel - 1.0F) < 0.01F;
        }

        #region File commands

        /// <summary>
        /// Executes the File/New Event command. Shows the New Event wizard.
        /// </summary>
        [RelayCommand]
        private async Task NewEvent()
        {
            bool closeSuccess = await controller.TryCloseFile();
            if (!closeSuccess)
                return;

            NewEventWizardDialogViewModel wizardVm = new NewEventWizardDialogViewModel();
            bool result = await Services.DialogService.ShowDialogAsync(wizardVm);
            if (result) {
                bool success = await controller.NewEvent(wizardVm.CreateEventInfo);
                if (!success) {
                    // Old file is gone and new file failed — show error, the controller
                    // will have reset to an empty state.
                    await Services.DialogService.ShowDialogAsync(new MessageBoxDialogViewModel {
                        Message = MiscText.NewEventFailed,
                        Icon = MessageBoxIcon.Error,
                        Buttons = MessageBoxButtons.Ok,
                        DefaultButton = MessageBoxButton.Ok
                    });
                }
            }
        }

        /// <summary>
        /// Shows the Open File dialog filtered to Purple Pen files (.ppen),
        /// and opens the selected file.
        /// </summary>
        [RelayCommand]
        private async Task FileOpenPurplePenFile()
        {
            if (controller == null) return;

#if PORTING
            // Not all functionality ported from MainFrame.openMenu_Click.
#endif
            FileOpenSingleViewModel fileOpenVM = new FileOpenSingleViewModel {
                FileFilters = MiscText.OpenFileDialog_PurplePenFilter,
                InitialFileFilterIndex = 1
            };

            bool result = await Services.DialogService.ShowDialogAsync(fileOpenVM);

            if (result && fileOpenVM.SelectedFile != null) {
                string newFilename = fileOpenVM.SelectedFile;
                bool success = await controller.LoadNewFile(newFilename);
            }
        }

        /// <summary>
        /// Executes the File/Save command.
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            if (controller == null) return;
            controller.Save();
        }

        /// <summary>
        /// Executes the File/Save As command. Shows a Save File dialog.
        /// </summary>
        [RelayCommand]
        private async Task SaveAs()
        {
            if (controller == null) return;
            string? newFileName = await Services.DialogService.ShowSaveFilePickerAsync(controller.FileName);
            if (newFileName != null)
                controller.SaveAs(newFileName);
        }

        /// <summary>
        /// Executes the File/Exit command. Requests window closure (triggers the Closing handler).
        /// </summary>
        [RelayCommand]
        private void Exit()
        {
            CloseRequested?.Invoke(this, System.EventArgs.Empty);
        }

        /// <summary>
        /// Called by the window Closing handler to check for unsaved changes.
        /// Returns true if it is safe to close (no changes, or user confirmed save/discard).
        /// </summary>
        public async Task<bool> TryCloseAsync()
        {
            if (controller == null) return true;
            return await controller.TryCloseFile();
        }

        #endregion // File commands

        #region Edit commands

        /// <summary>
        /// Executes the Edit/Cancel command. Cancels the current mode or clears selection.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            if (controller == null) return;
            if (controller.CanCancelMode())
                controller.CancelMode();
            else
                controller.ClearSelection();
        }

        /// <summary>
        /// Executes the Edit/Undo command.
        /// </summary>
        [RelayCommand]
        private void Undo()
        {
            if (controller == null) return;
            UndoStatus status = controller.GetUndoStatus();
            if (status.CanUndo) controller.Undo();
        }

        /// <summary>
        /// Executes the Edit/Redo command.
        /// </summary>
        [RelayCommand]
        private void Redo()
        {
            if (controller == null) return;
            UndoStatus status = controller.GetUndoStatus();
            if (status.CanRedo) controller.Redo();
        }

        /// <summary>
        /// Executes the Edit/Delete command. Deletes the current selection.
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelection()
        {
            if (controller == null) return;
            await controller.DeleteSelection();
        }

        /// <summary>
        /// Executes the Edit/Delete Fork command.
        /// </summary>
        [RelayCommand]
        private async Task DeleteFork()
        {
            if (controller == null) return;
            await controller.DeleteFork();
        }

        #endregion // Edit commands

        #region View commands

        /// <summary>
        /// Executes the View/Entire Course command. Zooms to show the entire course.
        /// </summary>
        [RelayCommand]
        private void ViewEntireCourse()
        {
            if (controller == null) return;
            RectangleF courseBounds = controller.GetCourseBounds();
            ShowRectangleCallback?.Invoke(courseBounds);
        }

        /// <summary>
        /// Executes the View/Entire Map command. Zooms to show the entire map.
        /// </summary>
        [RelayCommand]
        private void ViewEntireMap()
        {
            if (controller == null || MapDisplay == null) return;
            RectangleF mapBounds = MapDisplay.MapBounds;
            ShowRectangleCallback?.Invoke(mapBounds);
        }

        /// <summary>
        /// Sets the zoom factor. Called from zoom menu items via CommandParameter.
        /// </summary>
        [RelayCommand]
        private void SetZoom(double zoomFactor)
        {
            MapZoomFactor = (float)zoomFactor;
        }

        // Bindable properties to indicate if a zoom level menu item should be checked.
        [ObservableProperty] private bool zoom50Checked;
        [ObservableProperty] private bool zoom100Checked;
        [ObservableProperty] private bool zoom150Checked;
        [ObservableProperty] private bool zoom200Checked;
        [ObservableProperty] private bool zoom300Checked;
        [ObservableProperty] private bool zoom500Checked;
        [ObservableProperty] private bool zoom1000Checked;


        /// <summary>
        /// Sets the map intensity. Called from intensity menu items via CommandParameter.
        /// </summary>
        [RelayCommand]
        private void SetMapIntensity(double intensity)
        {
            if (MapDisplay == null) { return; }

            MapDisplay.MapIntensity = (float)intensity;
            UserSettings.Current.MapIntensity = MapDisplay.MapIntensity;
            UserSettings.Current.Save();
        }

        [ObservableProperty] private bool intensityVeryLowChecked;
        [ObservableProperty] private bool intensityLowChecked;
        [ObservableProperty] private bool intensityMediumChecked;
        [ObservableProperty] private bool intensityHighChecked;
        [ObservableProperty] private bool intensityFullChecked;


        /// <summary>
        /// Toggles display of popup information.
        /// </summary>
        [RelayCommand]
        private void ToggleShowPopups()
        {
            UserSettings.Current.ShowPopupInfo = !UserSettings.Current.ShowPopupInfo;
            UserSettings.Current.Save();
        }

        /// <summary>
        /// Toggles display of the print area.
        /// </summary>
        [RelayCommand]
        private void ToggleShowPrintArea()
        {
            if (controller == null) return;
            UserSettings.Current.ShowPrintArea = !UserSettings.Current.ShowPrintArea;
            UserSettings.Current.Save();
            controller.ForceChangeUpdate(true);
        }

        /// <summary>
        /// Sets map rendering to high quality (anti-aliased).
        /// </summary>
        [RelayCommand]
        private void SetHighQuality()
        {
            SetQuality(true);
        }

        /// <summary>
        /// Sets map rendering to normal quality.
        /// </summary>
        [RelayCommand]
        private void SetNormalQuality()
        {
            SetQuality(false);
        }

        private void SetQuality(bool highQuality)
        {
            if (MapDisplay == null) { return; }

            MapDisplay.AntiAlias = highQuality;
            UserSettings.Current.MapHighQuality = highQuality;
            UserSettings.Current.Save();
        }


        [ObservableProperty]
        bool highQualityMapDisplay;

        /// <summary>
        /// Toggles the "show all controls" view mode.
        /// </summary>
        [RelayCommand]
        private void ToggleAllControls()
        {
            if (controller == null) { return; }

            controller.ShowAllControls = !controller.ShowAllControls;
            UserSettings.Current.ViewAllControls = controller.ShowAllControls;
            UserSettings.Current.Save();
        }

        [ObservableProperty]
        bool viewAllControlsChecked;

        /// <summary>
        /// Shows the View Additional Courses dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowOtherCourses()
        {
            if (controller == null) return;

            ViewAdditionalCoursesDialogViewModel vm = new ViewAdditionalCoursesDialogViewModel();
            vm.Initialize(controller.GetEventDB(), controller.CurrentTabName,
                          controller.CurrentCourseId, controller.ExtraCourseDisplay);

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.ExtraCourseDisplay = vm.GetSelectedCourseIds();
        }

        /// <summary>
        /// Clears the extra course display.
        /// </summary>
        [RelayCommand]
        private void ClearOtherCourses()
        {
            if (controller == null) return;
            controller.ClearExtraCourseDisplay();
        }

        #endregion // View commands

        #region Add control commands

        /// <summary>
        /// Executes the Add/Control command. Begins adding a normal control.
        /// </summary>
        [RelayCommand]
        private void AddControl()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Normal, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Start command. Begins adding a start control.
        /// </summary>
        [RelayCommand]
        private void AddStart()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Start, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Finish command. Begins adding a finish control.
        /// </summary>
        [RelayCommand]
        private void AddFinish()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Finish, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Map Exchange at Control command.
        /// </summary>
        [RelayCommand]
        private void AddMapExchangeControl()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Normal, MapExchangeType.Exchange);
        }

        /// <summary>
        /// Executes the Add/Map Flip at Control command.
        /// </summary>
        [RelayCommand]
        private void AddMapFlipControl()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Normal, MapExchangeType.MapFlip);
        }

        /// <summary>
        /// Executes the Add/Map Exchange (Separate) command.
        /// </summary>
        [RelayCommand]
        private void AddMapExchangeSeparate()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.MapExchange, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Descriptions command. Begins adding a description block.
        /// </summary>
        [RelayCommand]
        private void AddDescriptions()
        {
            if (controller == null) { return; }

            controller.BeginAddDescriptionMode();
        }

        /// <summary>
        /// Executes the Add/Variation command. Shows the Add Fork dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddVariation()
        {
            if (controller == null) return;

            string reason;
            if (controller.CanAddVariation(out reason) != CommandStatus.Enabled) {
                await ErrorMessage(reason);
                return;
            }

            AddForkDialogViewModel vm = new AddForkDialogViewModel();
            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            await controller.AddVariation(vm.Loop, vm.NumberOfBranches);
        }

        /// <summary>
        /// Executes the Add/Text Line command. Shows the Add Text Line dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddTextLine()
        {
            if (controller == null) return;

            string defaultText;
            DescriptionLine.TextLineKind defaultLineKind;
            string objectName;
            bool enableThisCourse;

            if (!controller.CanAddTextLine(out defaultText, out defaultLineKind, out objectName, out enableThisCourse))
                return;

            AddTextLineDialogViewModel vm = new AddTextLineDialogViewModel(objectName, enableThisCourse);
            vm.SetTextLine(defaultText);
            vm.TextLineKind = defaultLineKind;

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.AddTextLine(vm.GetTextLine(), vm.TextLineKind);
        }

        #endregion // Add control commands

        #region Add special item commands

        /// <summary>
        /// Executes the Add/Map Issue command. Shows the Map Issue Choice dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddMapIssue()
        {
            if (controller == null) return;

            MapIssueChoiceDialogViewModel vm = new MapIssueChoiceDialogViewModel();
            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.BeginAddMapIssuePointMode(vm.SelectedKind);
        }

        /// <summary>
        /// Executes the Add/Mandatory Crossing command.
        /// </summary>
        [RelayCommand]
        private void AddMandatoryCrossing()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.CrossingPoint, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Out of Bounds command.
        /// </summary>
        [RelayCommand]
        private void AddOutOfBounds()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.OOB, true);
        }

        /// <summary>
        /// Executes the Add/Dangerous command.
        /// </summary>
        [RelayCommand]
        private void AddDangerous()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.Dangerous, true);
        }

        /// <summary>
        /// Executes the Add/Construction command.
        /// </summary>
        [RelayCommand]
        private void AddConstruction()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.Construction, true);
        }

        /// <summary>
        /// Executes the Add/Boundary command.
        /// </summary>
        [RelayCommand]
        private void AddBoundary()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.Boundary, false);
        }

        /// <summary>
        /// Executes the Add/Optional Crossing command.
        /// </summary>
        [RelayCommand]
        private void AddOptCrossing()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.OptCrossing);
        }

        /// <summary>
        /// Executes the Add/Water command.
        /// </summary>
        [RelayCommand]
        private void AddWater()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.Water);
        }

        /// <summary>
        /// Executes the Add/First Aid command.
        /// </summary>
        [RelayCommand]
        private void AddFirstAid()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.FirstAid);
        }

        /// <summary>
        /// Executes the Add/Forbidden Route command.
        /// </summary>
        [RelayCommand]
        private void AddForbidden()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.Forbidden);
        }

        /// <summary>
        /// Executes the Add/Registration Mark command.
        /// </summary>
        [RelayCommand]
        private void AddRegMark()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.RegMark);
        }

        /// <summary>
        /// Executes the Add/White Out command.
        /// </summary>
        [RelayCommand]
        private void AddWhiteOut()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.WhiteOut, true);
        }

        /// <summary>
        /// Executes the Add/Text command. Shows the Change Text dialog for adding text.
        /// </summary>
        [RelayCommand]
        private async Task AddText()
        {
            if (controller == null) return;

            short colorOcadId;
            float c, m, y, k;
            bool purpleOverprint;
            string fontName;
            bool fontBold, fontItalic;
            float fontHeight;
            bool fontAutoSize;
            SpecialColor fontColor;

            FindPurple.GetPurpleColor(mapDisplay, controller.GetCourseAppearance(), out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

            var vm = new ChangeTextDialogViewModel {
                Title = MiscText.AddTextSpecialTitle,
                Explanation = MiscText.AddTextSpecialExplanation,
                ShowInsertSpecial = true
            };
            vm.InitializeColors(CmykColor.FromCmyk(c, m, y, k));

            controller.GetAddTextDefaultProperties(out fontName, out fontBold, out fontItalic, out fontColor, out fontHeight, out fontAutoSize);
            vm.FontName = fontName;
            vm.FontBold = fontBold;
            vm.FontItalic = fontItalic;
            vm.FontSize = (decimal)(fontHeight < 0 ? 5.0f : fontHeight);
            vm.FontSizeAutomatic = fontAutoSize;
            vm.SetSelectedColor(fontColor);

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.BeginAddTextSpecialMode(vm.UserText, vm.FontName, vm.FontBold, vm.FontItalic,
                    vm.GetSelectedColor(), vm.FontSizeAutomatic ? -1f : (float)vm.FontSize);
            }
        }

        /// <summary>
        /// Executes the Add/Image command. Shows an Open File dialog for image selection.
        /// </summary>
        [RelayCommand]
        private async Task AddImage()
        {
            if (controller == null) return;

            FileOpenSingleViewModel fileOpenVM = new FileOpenSingleViewModel {
                FileFilters = MiscText.OpenImageDialog_Filter,
            };
            if (!await Services.DialogService.ShowDialogAsync(fileOpenVM))
                return;
            if (fileOpenVM.SelectedFile != null)
                controller.BeginAddImageSpecialMode(fileOpenVM.SelectedFile);
        }

        /// <summary>
        /// Executes the Add/Line command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddLine()
        {
            if (controller == null) return;

            controller.GetLineSpecialProperties(SpecialKind.Line, false,
                out SpecialColor color, out LineKind lineKind,
                out float lineWidth, out float gapSize, out float dashSize, out float _);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                Title = MiscText.AddLineTitle,
                Explanation = MiscText.AddLineExplanation,
                ShowRadius = false,
                ShowLineKind = true,
                Color = color,
                LineKind = lineKind,
                LineWidth = (decimal)lineWidth,
                GapSize = (decimal)gapSize,
                DashSize = (decimal)dashSize,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.BeginAddLineSpecialMode(vm.Color, vm.LineKind,
                (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize);
        }

        /// <summary>
        /// Executes the Add/Rectangle command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddRectangle()
        {
            if (controller == null) return;

            controller.GetLineSpecialProperties(SpecialKind.Rectangle, false,
                out SpecialColor color, out LineKind _, out float lineWidth,
                out float gapSize, out float dashSize, out float cornerRadius);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                Title = MiscText.AddRectangleTitle,
                Explanation = MiscText.AddRectangleExplanation,
                ShowRadius = true,
                ShowLineKind = false,
                Color = color,
                LineKind = LineKind.Single,
                LineWidth = (decimal)lineWidth,
                GapSize = (decimal)gapSize,
                DashSize = (decimal)dashSize,
                CornerRadius = (decimal)cornerRadius,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.BeginAddRectangleSpecialMode(false, vm.Color, vm.LineKind,
                (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize, (float)vm.CornerRadius);
        }

        /// <summary>
        /// Executes the Add/Ellipse command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddEllipse()
        {
            if (controller == null) return;

            controller.GetLineSpecialProperties(SpecialKind.Ellipse, false,
                out SpecialColor color, out LineKind lineKind,
                out float lineWidth, out float gapSize, out float dashSize, out float _);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                Title = MiscText.AddEllipseTitle,
                Explanation = MiscText.AddEllipseExplanation,
                ShowRadius = false,
                ShowLineKind = true,
                Color = color,
                LineKind = LineKind.Single,
                LineWidth = (decimal)lineWidth,
                GapSize = (decimal)gapSize,
                DashSize = (decimal)dashSize,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.BeginAddRectangleSpecialMode(true, vm.Color, vm.LineKind,
                (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize, 0);
        }

        #endregion // Add special item commands

        #region Item modification commands

        /// <summary>
        /// Executes the Item/Add Bend command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddBend))]
        private void AddBend()
        {
            if (controller == null) { return; }
            controller.BeginAddBend();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddBendCommand))]
        private bool canAddBend;


        /// <summary>
        /// Executes the Item/Remove Bend command.
        /// </summary>
        [RelayCommand]
        private void RemoveBend()
        {
            if (controller == null) { return; }

            controller.BeginRemoveBend();
        }

        /// <summary>
        /// Executes the Item/Add Gap command.
        /// </summary>
        [RelayCommand]
        private void AddGap()
        {
            if (controller == null) { return; }

            controller.BeginAddGap();
        }

        /// <summary>
        /// Executes the Item/Remove Gap command.
        /// </summary>
        [RelayCommand]
        private void RemoveGap()
        {
            if (controller == null) { return; }

            controller.BeginRemoveGap();
        }

        /// <summary>
        /// Executes the Item/Rotate command.
        /// </summary>
        [RelayCommand]
        private void Rotate()
        {
            if (controller == null) { return; }

            controller.BeginRotate();
        }

        /// <summary>
        /// Executes the Item/Stretch command.
        /// </summary>
        [RelayCommand]
        private void Stretch()
        {
            if (controller == null) { return; }

            controller.BeginStretch();
        }

        /// <summary>
        /// Executes the Item/Change Text command. Shows the Change Text dialog.
        /// </summary>
        [RelayCommand]
        private async Task ChangeText()
        {
            if (controller == null) return;
            if (controller.CanChangeText() != CommandStatus.Enabled) return;

            short colorOcadId;
            float c, m, y, k;
            bool purpleOverprint;
            string fontName;
            bool fontBold, fontItalic;
            float fontHeight;
            SpecialColor fontColor;
            FindPurple.GetPurpleColor(mapDisplay, controller.GetCourseAppearance(), out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

            string oldText = controller.GetChangableText();
            controller.GetChangableTextProperties(out fontName, out fontBold, out fontItalic, out fontColor, out fontHeight);

            var vm = new ChangeTextDialogViewModel {
                Title = MiscText.ChangeTextTitle,
                Explanation = MiscText.ChangeTextSpecialExplanation,
                ShowInsertSpecial = true,
                UserText = oldText,
                FontName = fontName,
                FontBold = fontBold,
                FontItalic = fontItalic,
                FontSize = (decimal)(fontHeight < 0 ? 5f : fontHeight),
                FontSizeAutomatic = (fontHeight < 0)
            };
            vm.InitializeColors(CmykColor.FromCmyk(c, m, y, k));
            vm.SetSelectedColor(fontColor);

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.ChangeText(vm.UserText, vm.FontName, vm.FontBold, vm.FontItalic,
                    vm.GetSelectedColor(), vm.FontSizeAutomatic ? -1f : (float)vm.FontSize);
            }
        }

        /// <summary>
        /// Executes the Item/Change Line Appearance command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task ChangeLineAppearance()
        {
            if (controller == null) return;
            if (controller.CanChangeLineAppearance() != CommandStatus.Enabled) return;

            controller.GetChangableLineProperties(out bool showRadius, out SpecialColor color,
                out LineKind lineKind, out float lineWidth, out float gapSize,
                out float dashSize, out float cornerRadius);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                Title = MiscText.ChangeLineAppearanceTitle,
                Explanation = MiscText.ChangeLineAppearanceExplanation,
                ShowRadius = showRadius,
                ShowLineKind = !showRadius,
                Color = color,
                LineKind = lineKind,
                LineWidth = (decimal)lineWidth,
                GapSize = (decimal)gapSize,
                DashSize = (decimal)dashSize,
                CornerRadius = (decimal)cornerRadius,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.ChangeLineAppearance(vm.Color, vm.LineKind,
                (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize, (float)vm.CornerRadius);
        }

        /// <summary>
        /// Executes the Item/Change Displayed Courses command.
        /// </summary>
        [RelayCommand]
        private async Task ChangeDisplayedCourses()
        {
            if (controller == null) return;
            if (controller.CanChangeDisplayedCourses(out CourseDesignator[] displayedCourses,
                                                     out bool showAllControls) != CommandStatus.Enabled)
                return;

            ChangeDisplayedCoursesDialogViewModel vm = new ChangeDisplayedCoursesDialogViewModel();
            vm.Initialize(controller.GetEventDB(), showAllControls, displayedCourses);

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.ChangeDisplayedCourses(vm.GetSelectedDesignators());
        }

        #endregion // Item modification commands

        #region Leg flagging commands

        /// <summary>
        /// Executes the Leg/No Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetNoFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.None);
        }

        /// <summary>
        /// Executes the Leg/Entire Leg Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetEntireFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.All);
        }

        /// <summary>
        /// Executes the Leg/Begin Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetBeginFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.Begin);
        }

        /// <summary>
        /// Executes the Leg/End Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetEndFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.End);
        }

        #endregion // Leg flagging commands

        #region Course commands

        /// <summary>
        /// Shows the Add Course dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowAddCourseDialog()
        {
            if (controller == null) return;

            AddCourseDialogViewModel vm = new AddCourseDialogViewModel();
            vm.InitializePrintScales(controller.MapScale);

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.NewCourse(
                vm.CourseKind,
                vm.CourseName,
                vm.ControlLabelKind,
                vm.ScoreColumn,
                vm.SecondaryTitlePipeDelimited ?? "",
                vm.PrintScale,
                vm.Climb,
                vm.Length,
                vm.DescKind,
                vm.FirstControlOrdinal,
                vm.HideFromReports);
        }

        /// <summary>
        /// Executes the Course/Delete Course command.
        /// </summary>
        [RelayCommand]
        private async Task DeleteCourse()
        {
            if (controller == null) return;

            await controller.DeleteCurrentCourse();
        }

        /// <summary>
        /// Executes the Course/Duplicate Course command. Shows the Add Course dialog
        /// pre-populated with current course properties.
        /// </summary>
        [RelayCommand]
        private async Task DuplicateCourse()
        {
            if (controller == null || !controller.CanDuplicateCurrentCourse()) return;

            controller.GetCurrentCourseProperties(
                out CourseKind courseKind, out string _, out ControlLabelKind labelKind,
                out int scoreColumn, out string secondaryTitle, out float printScale,
                out float climb, out float? length, out DescriptionKind descKind,
                out int firstControlOrdinal, out bool hideFromReports);

            AddCourseDialogViewModel vm = new AddCourseDialogViewModel();
            vm.InitializePrintScales(controller.MapScale);
            vm.CourseKind = courseKind;
            vm.CourseName = "";
            vm.SecondaryTitlePipeDelimited = secondaryTitle;
            vm.PrintScale = printScale;
            vm.Climb = climb;
            vm.Length = length;
            vm.DescKind = descKind;
            vm.FirstControlOrdinal = firstControlOrdinal;
            vm.ControlLabelKind = labelKind;
            vm.ScoreColumn = scoreColumn;
            vm.HideFromReports = hideFromReports;
            vm.CanChangeCourseKind = false;

            if (!await Services.DialogService.ShowDialogAsync(vm)) return;

            controller.DuplicateCurrentCourse(
                vm.CourseName, vm.ControlLabelKind, vm.ScoreColumn,
                vm.SecondaryTitlePipeDelimited ?? "", vm.PrintScale, vm.Climb, vm.Length,
                vm.DescKind, vm.FirstControlOrdinal, vm.HideFromReports);
        }

        /// <summary>
        /// Executes the Course/Properties command. Shows the course properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseProperties()
        {
            if (controller == null) return;

            if (controller.CanChangeCourseProperties()) {
                controller.GetCurrentCourseProperties(
                    out CourseKind courseKind, out string courseName, out ControlLabelKind labelKind,
                    out int scoreColumn, out string secondaryTitle, out float printScale,
                    out float climb, out float? length, out DescriptionKind descKind,
                    out int firstControlOrdinal, out bool hideFromReports);

                AddCourseDialogViewModel vm = new AddCourseDialogViewModel();
                vm.InitializePrintScales(controller.MapScale);
                vm.CourseKind = courseKind;
                vm.CourseName = courseName;
                vm.SecondaryTitlePipeDelimited = secondaryTitle;
                vm.PrintScale = printScale;
                vm.Climb = climb;
                vm.Length = length;
                vm.DescKind = descKind;
                vm.FirstControlOrdinal = firstControlOrdinal;
                vm.ControlLabelKind = labelKind;
                vm.ScoreColumn = scoreColumn;
                vm.HideFromReports = hideFromReports;

                if (!await Services.DialogService.ShowDialogAsync(vm)) return;

                controller.ChangeCurrentCourseProperties(
                    vm.CourseKind, vm.CourseName, vm.ControlLabelKind, vm.ScoreColumn,
                    vm.SecondaryTitlePipeDelimited ?? "", vm.PrintScale, vm.Climb, vm.Length,
                    vm.DescKind, vm.FirstControlOrdinal, vm.HideFromReports);
            }
            else {
                controller.GetAllControlsProperties(out float printScale, out DescriptionKind descKind);
                AllControlsPropertiesDialogViewModel vm =
                    new AllControlsPropertiesDialogViewModel(controller.MapScale, printScale, descKind);
                if (!await Services.DialogService.ShowDialogAsync(vm)) return;
                controller.ChangeAllControlsProperties(vm.PrintScale, vm.DescKind);
            }
        }

        /// <summary>
        /// Executes the Course/Course Load command. Shows the Course Load dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseLoad()
        {
            if (controller == null) return;
            CourseLoadDialogViewModel vm = new CourseLoadDialogViewModel(controller);
            if (!await Services.DialogService.ShowDialogAsync(vm)) return;
            controller.SetAllCourseLoads(vm.GetCourseLoads());
        }

        /// <summary>
        /// Executes the Course/Course Order command. Shows the Change Course Order dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseOrder()
        {
            if (controller == null) return;
            CourseOrderDialogViewModel vm = new CourseOrderDialogViewModel(controller);
            if (!await Services.DialogService.ShowDialogAsync(vm)) return;
            controller.SetAllCourseOrders(vm.GetCourseOrders());
        }

        /// <summary>
        /// Executes the Course/Course Variation Report command.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseVariationReport()
        {
            if (controller == null) return;

            RelaySettings? relaySettings = controller.GetRelayParameters();
            if (relaySettings == null) return;
            bool hideVariationsOnMap = controller.GetHideVariationsOnMap();

            TeamVariationsDialogViewModel vm = new TeamVariationsDialogViewModel(relaySettings, hideVariationsOnMap);
            vm.DefaultExportFileName = controller.GetDefaultVariationExportFileName();
            vm.Controller = controller;

            await Services.DialogService.ShowDialogAsync(vm);

            // Always save parameters (the dialog has no Cancel — only Close).
            controller.SetRelayParameters(vm.RelaySettings, vm.HideVariationsOnMap);
        }

        #endregion // Course commands

        #region Event/tools commands

        /// <summary>
        /// Executes the Event/Change Map File command. Shows the Change Map File dialog.
        /// </summary>
        [RelayCommand]
        private async Task ChangeMapFile()
        {
            if (controller == null) return;

            ChangeMapFileDialogViewModel vm = new ChangeMapFileDialogViewModel();
            // Setting MapFile triggers ValidateMapFile and pre-populates scale/dpi.
            vm.MapFile = controller.MapFileName ?? "";
            // Allow the user to override scale/dpi when the map already has values.
            if (controller.MapType == MapType.Bitmap) {
                vm.MapScaleText = controller.MapScale.ToString();
                vm.DpiText = controller.MapDpi.ToString();
            }
            else if (controller.MapType == MapType.PDF) {
                vm.MapScaleText = controller.MapScale.ToString();
            }

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.ChangeMapFile(vm.MapType, vm.MapFile, vm.MapScale, vm.Dpi);
        }

        /// <summary>
        /// Executes the Event/Change Codes command. Shows the Change All Codes dialog.
        /// </summary>
        [RelayCommand]
        private async Task ChangeCodes()
        {
            if (controller == null) return;

            ChangeAllCodesDialogViewModel vm = new ChangeAllCodesDialogViewModel();
            vm.SetEventDB(controller.GetEventDB());
            vm.SetCodes(controller.GetAllControlCodes());

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.SetAllControlCodes(vm.GetCodes());
        }

        /// <summary>
        /// Executes the Event/Auto Numbering command. Shows the Auto Numbering dialog.
        /// </summary>
        [RelayCommand]
        private async Task AutoNumbering()
        {
            if (controller == null) return;

            controller.GetAutoNumbering(out int firstCode, out bool disallowInvertibleCodes);

            AutoNumberingDialogViewModel vm = new AutoNumberingDialogViewModel(firstCode, disallowInvertibleCodes);
            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.AutoNumbering(vm.FirstCode, vm.DisallowInvertibleCodes, vm.RenumberExisting);
        }

        /// <summary>
        /// Executes the Event/Remove Unused Controls command.
        /// </summary>
        [RelayCommand]
        private async Task RemoveUnusedControls()
        {
            if (controller == null) return;

            List<KeyValuePair<Id<ControlPoint>, string>> unusedControls = controller.GetUnusedControls();

            if (unusedControls.Count == 0) {
                await InfoMessage(MiscText.NoUnusedControls);
                return;
            }

            UnusedControlsDialogViewModel vm = new UnusedControlsDialogViewModel();
            vm.SetControlsToDelete(unusedControls);

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            controller.RemoveControls(vm.GetControlsToDelete());
        }

        /// <summary>
        /// Executes the Event/Move All Controls command.
        /// </summary>
        [RelayCommand]
        private void MoveAllControls()
        {
#if !PORTING
            // Part 1: Determine which action we are doing.
            MoveAllControls moveAllControlsDialog = new MoveAllControls();
            if (moveAllControlsDialog.ShowDialog() == DialogResult.Cancel) {
                moveAllControlsDialog.Dispose();
                return;
            }

            MoveAllControlsAction action = moveAllControlsDialog.Action;
            moveAllControlsDialog.Dispose();

            // Part 2: Prompt use to move controls
            controller.BeginMoveAllControls();

            SelectLocationsForMove selectLocationsForMoveDialog = new SelectLocationsForMove(controller, action);
            Point location = this.Location;
            location.Offset(10, 130);
            selectLocationsForMoveDialog.Location = location;
            selectLocationsForMoveDialog.Show(this);

            // Dialog dismisses/disposes itself and invokes controller.
#endif
        }

        /// <summary>
        /// Executes the Event/Punch Patterns command. Shows the Punch Pattern dialog.
        /// </summary>
        [RelayCommand]
        private async Task PunchPatterns()
        {
            Dictionary<string, PunchPattern> allPatterns = controller.GetAllPunchPatterns();
            PunchcardFormat punchcardFormat = controller.GetPunchcardFormat();

            PunchPatternDialogViewModel vm = new PunchPatternDialogViewModel();
            vm.Initialize(allPatterns, punchcardFormat);

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                PunchcardFormat newFormat = vm.GetPunchcardFormat();
                if (!newFormat.Equals(punchcardFormat))
                    controller.SetPunchcardFormat(newFormat);
                controller.SetAllPunchPatterns(vm.GetAllPunchPatterns());
            }
        }

        /// <summary>
        /// Executes the Event/Customize Descriptions command. Shows the Custom Symbol Text dialog.
        /// </summary>
        [RelayCommand]
        private void CustomizeDescriptions()
        {
#if !PORTING
            Dictionary<string, List<SymbolText>> customSymbolText;
            Dictionary<string, bool> customSymbolKey;

            // Initialize the dialog
            CustomSymbolText dialog = new CustomSymbolText(symbolDB, false);
            controller.GetCustomSymbolText(out customSymbolText, out customSymbolKey);
            dialog.SetCustomSymbolDictionaries(customSymbolText, customSymbolKey);
            dialog.LangId = controller.GetDescriptionLanguage();

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply the changes
            if (result == DialogResult.OK) {
                // dialog changes the dictionaries, so we don't need to retrieve them.
                controller.SetCustomSymbolText(customSymbolText, customSymbolKey, dialog.LangId);
                if (dialog.UseAsDefaultLanguage)
                    controller.DefaultDescriptionLanguage = dialog.LangId;
            }

            dialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Event/Customize Course Appearance command.
        /// </summary>
        [RelayCommand]
        private async Task CustomizeCourseAppearance()
        {
            // Get the default purple color from the map (pass null appearance to get map default).
            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(MapDisplay, null, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            bool usesOcadMap = MapDisplay?.MapType == MapType.OCAD;

            CourseAppearance appearance = controller.GetCourseAppearance();
            if (usesOcadMap && appearance.purpleColorBlend != PurpleColorBlend.UpperLowerPurple) {
                // Pre-select the default lower purple layer so it is ready when the user switches to Layer mode.
                appearance.mapLayerForLowerPurple = controller.GetDefaultLowerPurpleLayer();
            }

            CourseAppearanceDialogViewModel vm = new CourseAppearanceDialogViewModel();
            vm.Initialize(appearance, usesOcadMap, controller.GetUnderlyingMapColors(), c, m, y, k);

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.SetCourseAppearance(vm.GetCourseAppearance());
            }
        }

        #endregion // Event/tools commands

        #region IOF Standards commands

        /// <summary>
        /// Sets the description standard to 2004.
        /// </summary>
        [RelayCommand]
        private void SetDescriptionStd2004()
        {
            if (controller == null) { return; }

            controller.ChangeDescriptionStandard("2004");
        }

        /// <summary>
        /// Sets the description standard to 2018.
        /// </summary>
        [RelayCommand]
        private void SetDescriptionStd2018()
        {
            if (controller == null) { return; }

            controller.ChangeDescriptionStandard("2018");
        }

        /// <summary>
        /// Sets the map standard to 2000.
        /// </summary>
        [RelayCommand]
        private void SetMapStd2000()
        {
            if (controller == null) { return; }

            controller.ChangeMapStandard("2000");
        }

        /// <summary>
        /// Sets the map standard to 2017.
        /// </summary>
        [RelayCommand]
        private void SetMapStd2017()
        {
            if (controller == null) { return; }

            controller.ChangeMapStandard("2017");
        }

        /// <summary>
        /// Sets the map standard to Sprint 2019.
        /// </summary>
        [RelayCommand]
        private void SetMapStdSpr2019()
        {
            if (controller == null) { return; }

            controller.ChangeMapStandard("Spr2019");
        }

        #endregion // IOF Standards commands

        #region Print area commands

        /// <summary>
        /// Sets the print area for this part only.
        /// </summary>
        [RelayCommand]
        private void SetPrintAreaThisPart()
        {
#if !PORTING
            SetPrintArea(PrintAreaKind.OnePart);
#endif
        }

        /// <summary>
        /// Sets the print area for this course only.
        /// </summary>
        [RelayCommand]
        private void SetPrintAreaThisCourse()
        {
#if !PORTING
            SetPrintArea(PrintAreaKind.OneCourse);
#endif
        }

        /// <summary>
        /// Sets the print area for all courses.
        /// </summary>
        [RelayCommand]
        private void SetPrintAreaAllCourses()
        {
#if !PORTING
            SetPrintArea(PrintAreaKind.AllCourses);
#endif
        }

        #endregion // Print area commands

        #region Print and export commands

        /// <summary>
        /// Executes the File/Print Descriptions command.
        /// </summary>
        [RelayCommand]
        private void PrintDescriptions()
        {
#if !PORTING
            // Initialize dialog
            PrintDescriptions printDescDialog = new PrintDescriptions(controller.GetEventDB(), false);
            printDescDialog.controller = controller;
            printDescDialog.PrintSettings = descPrintSettings;
            printDescDialog.PrinterPageSettings = descPrintPageSettings;

            // show the dialog, on success, print.
            if (printDescDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                descPrintSettings = printDescDialog.PrintSettings;
                descPrintPageSettings = printDescDialog.PrinterPageSettings;
                controller.PrintDescriptions(WindowsUtil.GetWinFormsPrintTarget(descPrintPageSettings, this, false),
                    descPrintSettings, WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(descPrintPageSettings));
            }

            // And the dialog is done.
            printDescDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create Description PDF command.
        /// </summary>
        [RelayCommand]
        private void CreateDescriptionPdf()
        {
#if !PORTING
            // Initialize dialog
            PrintDescriptions printDescDialog = new PrintDescriptions(controller.GetEventDB(), true);
            printDescDialog.controller = controller;
            printDescDialog.PrintSettings = descPrintSettings;
            printDescDialog.PrinterPageSettings = descPrintPageSettings;

            // show the dialog, on success, print.
            if (printDescDialog.ShowDialog(this) == DialogResult.OK) {
                // Figure out filename
                SaveFileDialog savePdfDialog = new SaveFileDialog();
                savePdfDialog.Filter = MiscText.PdfFilter;
                savePdfDialog.FilterIndex = 1;
                savePdfDialog.DefaultExt = "pdf";
                savePdfDialog.OverwritePrompt = true;
                savePdfDialog.InitialDirectory = Path.GetDirectoryName(controller.FileName);

                if (savePdfDialog.ShowDialog(this) == DialogResult.OK) {
                    // Save the settings for the next invocation of the dialog.
                    descPrintSettings = printDescDialog.PrintSettings;
                    descPrintPageSettings = printDescDialog.PrinterPageSettings;
                    controller.CreateDescriptionsPdf(descPrintSettings, WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(descPrintPageSettings), savePdfDialog.FileName);
                }
            }

            // And the dialog is done.
            printDescDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Print Punch Cards command.
        /// </summary>
        [RelayCommand]
        private void PrintPunchCards()
        {
#if !PORTING
            PrintPunches printPunchesDialog = new PrintPunches(controller.GetEventDB(), false);
            printPunchesDialog.controller = controller;
            printPunchesDialog.PrintSettings = punchPrintSettings;
            printPunchesDialog.PrinterPageSettings = punchPrintPageSettings;
            printPunchesDialog.PrintSettings.Count = 1;

            // show the dialog, on success, print.
            if (printPunchesDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                punchPrintSettings = printPunchesDialog.PrintSettings;
                punchPrintPageSettings = printPunchesDialog.PrinterPageSettings;
                controller.PrintPunches(WindowsUtil.GetWinFormsPrintTarget(punchPrintPageSettings, this, false),
                                        punchPrintSettings,
                                        WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(punchPrintPageSettings));
            }

            // And the dialog is done.
            printPunchesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create Punchcard PDF command.
        /// </summary>
        [RelayCommand]
        private void CreatePunchcardPdf()
        {
#if !PORTING
            PrintPunches printPunchesDialog = new PrintPunches(controller.GetEventDB(), true);
            printPunchesDialog.controller = controller;
            printPunchesDialog.PrintSettings = punchPrintSettings;
            printPunchesDialog.PrinterPageSettings = punchPrintPageSettings;
            printPunchesDialog.PrintSettings.Count = 1;

            // show the dialog, on success, print.
            if (printPunchesDialog.ShowDialog(this) == DialogResult.OK) {
                // Figure out filename
                SaveFileDialog savePdfDialog = new SaveFileDialog();
                savePdfDialog.Filter = MiscText.PdfFilter;
                savePdfDialog.FilterIndex = 1;
                savePdfDialog.DefaultExt = "pdf";
                savePdfDialog.OverwritePrompt = true;
                savePdfDialog.InitialDirectory = Path.GetDirectoryName(controller.FileName);

                if (savePdfDialog.ShowDialog(this) == DialogResult.OK) {
                    // Save the settings for the next invocation of the dialog.
                    punchPrintSettings = printPunchesDialog.PrintSettings;
                    punchPrintPageSettings = printPunchesDialog.PrinterPageSettings;
                    controller.CreatePunchesPdf(punchPrintSettings, WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(punchPrintPageSettings), savePdfDialog.FileName);
                }
            }

            // And the dialog is done.
            printPunchesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Print Courses command.
        /// </summary>
        [RelayCommand]
        private void PrintCourses()
        {
#if !PORTING
            if (!CheckForNonRenderableObjects(false, true))
                return;

            PrintCourses printCoursesDialog = new PrintCourses(controller.GetEventDB(), controller.AnyMultipart());
            printCoursesDialog.controller = controller;
            printCoursesDialog.PrintSettings = coursePrintSettings;

#if XPS_PRINTING
            if (controller.MustRasterizePrinting) {
                // Force rasterization.
                coursePrintSettings.UseXpsPrinting = false;
                printCoursesDialog.PrintSettings = coursePrintSettings;
                printCoursesDialog.EnableRasterizeChoice = false;
            }
#endif // XPS_PRINTING

            printCoursesDialog.PrintSettings.Count = 1;

            // show the dialog, on success, print.
            if (printCoursesDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                coursePrintSettings = printCoursesDialog.PrintSettings;
                coursePrintPageSettings = printCoursesDialog.PageSettings;
                controller.PrintCourses(WindowsUtil.GetWinFormsPrintTarget(coursePrintPageSettings, this, false),
                                        coursePrintSettings,
                                        WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(coursePrintPageSettings));
            }

            // And the dialog is done.
            printCoursesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create Course PDF command.
        /// </summary>
        [RelayCommand]
        private async Task CreateCoursePdf()
        {
            bool isPdfMap = controller.MapType == MapType.PDF;

            CoursePdfSettings settings = coursePdfSettings?.Clone() ?? new CoursePdfSettings {
                fileDirectory = true,
                mapDirectory = false,
                outputDirectory = Path.GetDirectoryName(controller.FileName),
            };
            if (isPdfMap)
                settings.CropLargePrintArea = true;

            CreateCoursePdfDialogViewModel vm = new CreateCoursePdfDialogViewModel(controller, settings, enableCropToggle: !isPdfMap);
            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            CoursePdfSettings final = vm.GetSettings();
            if (!await ConfirmOverwriteAsync(controller.OverwritingPdfFiles(final)))
                return;

            coursePdfSettings = final;
            controller.CreateCoursePdfs(coursePdfSettings);
        }

        /// <summary>
        /// Executes the File/Create OCAD Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateOcadFiles()
        {
            MapFileFormatKind restrictToKind = controller.MapDisplay.MapType == MapType.OCAD
                ? controller.MapDisplay.MapVersion.kind
                : MapFileFormatKind.None;

            OcadCreationSettings settings;
            if (ocadCreationSettingsPrevious != null) {
                settings = ocadCreationSettingsPrevious.Clone();
                // If map type changed, reset to the current map format.
                if (restrictToKind != MapFileFormatKind.None && restrictToKind != ocadCreationSettingsPrevious.fileFormat.kind)
                    settings.fileFormat = controller.MapDisplay.MapVersion;
            }
            else {
                settings = new OcadCreationSettings {
                    fileDirectory = true,
                    mapDirectory = false,
                    outputDirectory = Path.GetDirectoryName(controller.FileName),
                    fileFormat = controller.MapDisplay.MapType == MapType.OCAD
                        ? controller.MapDisplay.MapVersion
                        : new MapFileFormat(MapFileFormatKind.OCAD, 8),
                };
            }

            // Capture the purple color from the current map before showing the dialog.
            FindPurple.GetPurpleColor(controller.MapDisplay, controller.GetCourseAppearance(),
                out settings.colorOcadId, out settings.cyan, out settings.magenta,
                out settings.yellow, out settings.black, out settings.purpleOverprint);

            CreateOcadFilesDialogViewModel vm = new CreateOcadFilesDialogViewModel(controller, settings, restrictToKind);
            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            OcadCreationSettings final = vm.GetSettings();
            // Restore purple color fields (not edited in dialog).
            final.colorOcadId = settings.colorOcadId;
            final.cyan = settings.cyan;
            final.magenta = settings.magenta;
            final.yellow = settings.yellow;
            final.black = settings.black;
            final.purpleOverprint = settings.purpleOverprint;

            if (!await ConfirmOverwriteAsync(controller.OverwritingOcadFiles(final)))
                return;

            List<string> warnings = controller.OcadFilesWarnings(final);
            foreach (string warning in warnings)
                await WarningMessage(warning);

            ocadCreationSettingsPrevious = final;
            bool success = controller.CreateOcadFiles(ocadCreationSettingsPrevious);

            if (controller.MapDisplay.MapType == MapType.Bitmap)
                await InfoMessage(MiscText.ClosePPBeforeLoadingOCAD);

#if !PORTING
            // The Windows Store version doesn't install Roboto fonts into the system.
            if (success) {
                if (controller.ShouldInstallRobotoFonts()) {
                    if (await YesNoQuestion(MiscText.AskInstallRobotoFonts, true)) {
                        bool installSucceeded = controller.InstallRobotoFonts();
                        if (!installSucceeded)
                            await ErrorMessage(MiscText.RobotoFontsInstallFailed);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Executes the File/Create Image Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateImageFiles()
        {
            bool worldFileEnabled = controller.BitmapFilesCanCreateWorldFile();

            BitmapCreationSettings settings = bitmapCreationSettingsPrevious?.Clone() ?? new BitmapCreationSettings {
                fileDirectory = true,
                mapDirectory = false,
                outputDirectory = Path.GetDirectoryName(controller.FileName),
                Dpi = 200,
                ColorModel = ColorModel.CMYK,
                ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png,
            };
            if (!worldFileEnabled)
                settings.WorldFile = false;

            CreateImageFilesDialogViewModel vm = new CreateImageFilesDialogViewModel(controller, settings, worldFileEnabled);
            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            BitmapCreationSettings final = vm.GetSettings();
            if (!await ConfirmOverwriteAsync(controller.OverwritingBitmapFiles(final)))
                return;

            bitmapCreationSettingsPrevious = final;
            controller.CreateBitmapFiles(bitmapCreationSettingsPrevious);
        }

        /// <summary>
        /// Executes the File/Create Route Gadget Files command.
        /// </summary>
        [RelayCommand]
        private void CreateRouteGadgetFiles()
        {
#if !PORTING
            RouteGadgetCreationSettings settings;
            if (routeGadgetCreationSettingsPrevious != null)
                settings = routeGadgetCreationSettingsPrevious.Clone();
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new RouteGadgetCreationSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
                settings.fileBaseName = Path.GetFileNameWithoutExtension(controller.FileName);
            }

            // Initialize the dialog.
            CreateRouteGadgetFiles createRouteGadgetFilesDialog = new CreateRouteGadgetFiles(controller.GetEventDB());
            createRouteGadgetFilesDialog.RouteGadgetCreationSettings = settings;

            // show the dialog; on success, create the files.
            while (createRouteGadgetFilesDialog.ShowDialog(this) == DialogResult.OK) {
                List<string> overwritingFiles = controller.OverwritingRouteGadgetFiles(createRouteGadgetFilesDialog.RouteGadgetCreationSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                routeGadgetCreationSettingsPrevious = createRouteGadgetFilesDialog.RouteGadgetCreationSettings;
                controller.CreateRouteGadgetFiles(createRouteGadgetFilesDialog.RouteGadgetCreationSettings);

                break;
            }

            // And the dialog is done.
            createRouteGadgetFilesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Export XML command.
        /// </summary>
        [RelayCommand]
        private void CreateXml()
        {
#if !PORTING
            // The default output for the XML is the same as the event file name, with xml extension.
            string xmlFileName = Path.ChangeExtension(controller.FileName, ".xml");

            saveXmlFileDialog.FileName = xmlFileName;
            DialogResult result = saveXmlFileDialog.ShowDialog();

            if (result == DialogResult.OK) {
                int version = 2;
                if (saveXmlFileDialog.FilterIndex == 2)
                    version = 3;
                controller.ExportXml(saveXmlFileDialog.FileName, mapDisplay.MapBounds, version);
            }
#endif
        }

        /// <summary>
        /// Executes the File/Export GPX command.
        /// </summary>
        [RelayCommand]
        private async Task CreateGpx()
        {
#if !PORTING
            // First check and give immediate message if we can't do coordinate mapping.
            string message;
            if (!controller.CanExportGpxOrKml(out message)) {
                await ErrorMessage(message);
                return;
            }

            GpxCreationSettings settings;
            if (gpxCreationSettingsPrevious != null)
                settings = gpxCreationSettingsPrevious.Clone();
            else {
                // Default settings
                settings = new GpxCreationSettings();
            }

            // Initialize the dialog.
            CreateGpx createGpxDialog = new CreateGpx(controller.GetEventDB());
            createGpxDialog.CreationSettings = settings;

            // show the dialog; on success, create the files.
            if (createGpxDialog.ShowDialog(this) == DialogResult.OK) {
                // Show common save dialog to choose output file name.
                string gpxFileName = Path.ChangeExtension(controller.FileName, ".gpx");

                saveGpxFileDialog.FileName = gpxFileName;
                DialogResult result = saveGpxFileDialog.ShowDialog();

                if (result == DialogResult.OK) {
                    gpxFileName = saveGpxFileDialog.FileName;

                    // Save settings persisted between invocations of this dialog.
                    gpxCreationSettingsPrevious = createGpxDialog.CreationSettings;
                    controller.ExportGpx(gpxFileName, createGpxDialog.CreationSettings);
                }
            }

            // And the dialog is done.
            createGpxDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create KML Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateKmlFiles()
        {
#if !PORTING
            // First check and give immediate message if we can't do coordinate mapping.
            string message;
            if (!controller.CanExportGpxOrKml(out message)) {
                await ErrorMessage(message);
                return;
            }

            ExportKmlSettings settings;
            if (exportKmlSettingsPrevious != null) {
                settings = exportKmlSettingsPrevious.Clone();
            }
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new ExportKmlSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
            }

            // Initialize the dialog.
            CreateKmlFiles createKmlFilesDialog = new CreateKmlFiles(controller.GetEventDB());
            createKmlFilesDialog.ExportKmlSettings = settings;

            // show the dialog; on success, create the files.
            while (createKmlFilesDialog.ShowDialog(this) == DialogResult.OK) {
                // Warn about files that will be overwritten.
                List<string> overwritingFiles = controller.OverwritingKmlFiles(createKmlFilesDialog.ExportKmlSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                exportKmlSettingsPrevious = createKmlFilesDialog.ExportKmlSettings;
                controller.CreateKmlFiles(createKmlFilesDialog.ExportKmlSettings);

                break;
            }

            // And the dialog is done.
            createKmlFilesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Publish to Livelox command.
        /// </summary>
        [RelayCommand]
        private void PublishToLivelox()
        {
#if !PORTING
            LiveloxPublishSettings settings;
            if (liveloxPublishSettingsPrevious != null)
            {
                settings = liveloxPublishSettingsPrevious.Clone();
            }
            else
            {
                settings = new LiveloxPublishSettings();
            }

            var publishToLiveloxDialog = new PublishToLiveloxDialog(controller, symbolDB, settings);
            publishToLiveloxDialog.InitializeImportableEvent(this, call =>
            {
                // must invoke on UI thread
                this.InvokeOnUiThread(() => {
                    controller.EndProgressDialog();
                    if (call.Success)
                    {
                        publishToLiveloxDialog.ShowDialog(this);
                        liveloxPublishSettingsPrevious = publishToLiveloxDialog.PublishSettings;
                    }
                    else
                    {
                        MessageBox.Show(this, call.Exception?.Message, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    publishToLiveloxDialog.Dispose();
                });
            });
#endif
        }

        #endregion // Print and export commands

        #region Report commands

        /// <summary>
        /// Shows the Course Summary report in the default browser.
        /// </summary>
        [RelayCommand]
        private void ShowCourseSummary()
        {
            if (controller == null) return;
            Reports reportGenerator = new Reports();
            OpenHtmlReport(reportGenerator.CreateCourseSummaryReport(controller.GetEventDB()));
        }

        /// <summary>
        /// Shows the Control Cross-Reference report in the default browser.
        /// </summary>
        [RelayCommand]
        private void ShowControlCrossref()
        {
            if (controller == null) return;
            Reports reportGenerator = new Reports();
            OpenHtmlReport(reportGenerator.CreateCrossReferenceReport(controller.GetEventDB()));
        }

        /// <summary>
        /// Shows the Control and Leg Load report in the default browser.
        /// </summary>
        [RelayCommand]
        private void ShowControlAndLegLoad()
        {
            if (controller == null) return;
            Reports reportGenerator = new Reports();
            OpenHtmlReport(reportGenerator.CreateLoadReport(controller.GetEventDB()));
        }

        /// <summary>
        /// Shows the Leg Lengths report in the default browser.
        /// </summary>
        [RelayCommand]
        private void ShowLegLengths()
        {
            if (controller == null) return;
            Reports reportGenerator = new Reports();
            OpenHtmlReport(reportGenerator.CreateLegLengthReport(controller.GetEventDB()));
        }

        /// <summary>
        /// Shows the Event Audit report in the default browser.
        /// </summary>
        [RelayCommand]
        private void ShowEventAudit()
        {
            if (controller == null) return;
            Reports reportGenerator = new Reports();
            OpenHtmlReport(reportGenerator.CreateEventAuditReport(controller.GetEventDB()));
        }

        // Writes HTML to a temp file and opens it in the default browser.
        private static void OpenHtmlReport(string html)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"pp_report_{Guid.NewGuid():N}.html");
            File.WriteAllText(tempFile, html, Encoding.UTF8);
            OpenUrl(tempFile);
        }

        #endregion // Report commands

        #region Help and web commands

        /// <summary>
        /// Shows the help table of contents.
        /// </summary>
        [RelayCommand]
        private void HelpContents()
        {
#if !PORTING
            ShowHelp(HelpNavigator.TableOfContents, null);
#endif
        }

        /// <summary>
        /// Opens the translated help web site.
        /// </summary>
        [RelayCommand]
        private void HelpTranslated()
        {
            OpenUrl(MiscText.TranslatedHelpWebSite);
        }

        /// <summary>
        /// Opens the main Purple Pen web site.
        /// </summary>
        [RelayCommand]
        private void OpenMainWebSite()
        {
            OpenUrl("http://purple-pen.org");
        }

        /// <summary>
        /// Opens the Purple Pen support web site.
        /// </summary>
        [RelayCommand]
        private void OpenSupportWebSite()
        {
            OpenUrl("http://purple-pen.org#support");
        }

        /// <summary>
        /// Opens the Purple Pen donate web site.
        /// </summary>
        [RelayCommand]
        private void OpenDonateWebSite()
        {
            OpenUrl("http://purple-pen.org#donate");
        }

        // Opens a URL in the default browser using the OS shell.
        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>
        /// Shows the About dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowAboutDialog()
        {
            AboutDialogViewModel aboutViewModel = new AboutDialogViewModel();
            await Services.DialogService.ShowDialogAsync(aboutViewModel);
        }

        /// <summary>
        /// Shows the Switch Language dialog and applies the selected language.
        /// </summary>
        [RelayCommand]
        private async Task ShowSwitchLanguageDialog()
        {
            string currentCode = Services.UILanguage.LanguageCode;
            SwitchLanguageDialogViewModel vm = new SwitchLanguageDialogViewModel(currentCode, SwitchLanguageDialogViewModel.CreateDefaultLanguages());
            bool result = await Services.DialogService.ShowDialogAsync(vm);

            if (result && vm.SelectedLanguage != null) {
                Services.UILanguage.LanguageCode = vm.SelectedLanguage.Code;
            }
        }

        #endregion // Help and web commands

        #region Localization commands

        /// <summary>
        /// Executes the Translate/Add Description Language command.
        /// </summary>
        [RelayCommand]
        private void AddDescriptionLanguage()
        {
#if !PORTING
            DebugUI.NewLanguage newLanguageDialog = new NewLanguage(symbolDB);

            if (newLanguageDialog.ShowDialog(this) == DialogResult.OK) {
                SymbolLanguage symLanguage = new SymbolLanguage(newLanguageDialog.LanguageName, newLanguageDialog.LangId, newLanguageDialog.PluralNouns,
                    newLanguageDialog.PluralModifiers, newLanguageDialog.GenderModifiers,
                    newLanguageDialog.GenderModifiers ? newLanguageDialog.Genders.Split(new string[] {",", " "}, StringSplitOptions.RemoveEmptyEntries) : new string[0],
                    newLanguageDialog.CaseModifiers,
                    newLanguageDialog.CaseModifiers ? newLanguageDialog.Cases.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries) : new string[0]);
                controller.AddDescriptionLanguage(symLanguage, newLanguageDialog.CopyFromLangId);
                controller.SetDescriptionLanguage(symLanguage.LangId);
            }
#endif
        }

        /// <summary>
        /// Executes the Translate/Add Translated Texts command.
        /// </summary>
        [RelayCommand]
        private void AddTranslatedTexts()
        {
#if !PORTING
            // Initialize the dialog
            CustomSymbolText dialog = new CustomSymbolText(symbolDB, true);
            dialog.LangId = controller.GetDescriptionLanguage();

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply the changes
            if (result == DialogResult.OK) {
                controller.AddDescriptionTexts(dialog.CustomSymbolTexts, dialog.SymbolNames);
                controller.SetDescriptionLanguage(dialog.LangId);
            }

            dialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Translate/Merge Symbols command.
        /// </summary>
        [RelayCommand]
        private void MergeSymbols()
        {
#if !PORTING
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = ".xml";
            if (openFile.ShowDialog() == DialogResult.OK) {
                string filename = openFile.FileName;
                string langId = Microsoft.VisualBasic.Interaction.InputBox("Language code to import", "Merge Symbols.xml", null, 0, 0);
                controller.MergeSymbolsXml(filename, langId);
            }

            openFile.Dispose();
#endif
        }

        #endregion // Localization commands

        #region Debug commands

        /// <summary>
        /// Shows the Symbol Browser debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowSymbolBrowser()
        {
#if !PORTING
            SymbolBrowser symbolBrowser = new SymbolBrowser();
            symbolBrowser.Initialize(symbolDB);
            symbolBrowser.ShowDialog();
            symbolBrowser.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Description Browser debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowDescriptionBrowser()
        {
#if !PORTING
            DescriptionBrowser browser = new DescriptionBrowser();
            browser.Initialize(controller.GetEventDB(), symbolDB);
            browser.ShowDialog();
            browser.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Control Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowControlTester()
        {
#if !PORTING
            ControlTester controlTester = new ControlTester();
            controlTester.Initialize(controller.GetEventDB(), symbolDB);
            controlTester.ShowDialog();
            controlTester.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Map Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowMapTester()
        {
#if !PORTING
            MapTester mapTester = new MapTester();
            mapTester.ShowDialog();
            mapTester.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Course Selector Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowCourseSelectorTester()
        {
#if !PORTING
            new CourseSelectorTestForm(controller.GetEventDB()).ShowDialog(this);
#endif
        }

        /// <summary>
        /// Shows the Dot Grid Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowDotGridTester()
        {
#if !PORTING
            new DotGridTester().ShowDialog(this);
#endif
        }

        /// <summary>
        /// Shows the Dump OCAD File debug dialog.
        /// </summary>
        [RelayCommand]
        private void DumpOcadFile()
        {
#if !PORTING
            OpenFileDialog openOcadFileDialog = new OpenFileDialog();
            openOcadFileDialog.Filter = "OCAD files|*.ocd|All files|*.*";
            openOcadFileDialog.FilterIndex = 1;
            openOcadFileDialog.DefaultExt = "ocd";

            DialogResult result = openOcadFileDialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;
            string ocadFile = openOcadFileDialog.FileName;

            SaveFileDialog saveDumpFileDialog = new SaveFileDialog();
            saveDumpFileDialog.Filter = "Test file|*.txt";
            saveDumpFileDialog.FilterIndex = 1;
            saveDumpFileDialog.DefaultExt = "txt";

            result = saveDumpFileDialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;
            string dumpFile = saveDumpFileDialog.FileName;

            using (TextWriter writer = new StreamWriter(dumpFile)) {
                PurplePen.MapModel.DebugCode.OcadDump dumper = new PurplePen.MapModel.DebugCode.OcadDump();
                dumper.DumpFile(ocadFile, writer);
            }
#endif
        }

        /// <summary>
        /// Shows the Report Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowReportTester()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateTestReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm("Test Report", "", testReport, "PurplePenWindow.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Font Metrics debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowFontMetrics()
        {
#if !PORTING
            ShowFontMetrics fontMetricsDialog = new ShowFontMetrics(new GDIPlus_TextMetrics());

            fontMetricsDialog.ShowDialog(this);
            fontMetricsDialog.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Missing Translations debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowMissingTranslations()
        {
#if !PORTING
            UntranslatedSymbolTexts untranslatedSymbolTexts = new UntranslatedSymbolTexts();
            string report = untranslatedSymbolTexts.ReportOnUntranslatedSymbolTexts(symbolDB);

            DebugTextForm debugTextForm = new DebugTextForm("Missing Translations", report);
            debugTextForm.ShowDialog(this);
            debugTextForm.Dispose();
#endif
        }

        /// <summary>
        /// Intentional crash for testing error handling.
        /// </summary>
        [RelayCommand]
        private void TriggerCrash()
        {
#if !PORTING
            int x = 0;
            int y = 5 / x;
#endif
        }

        /// <summary>
        /// Test: shows a message box with OK button and Information icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxOk()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is an informational message with an OK button.",
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Information
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Test: shows a message box with OK/Cancel buttons and Warning icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxOkCancel()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is a warning message with OK and Cancel buttons.",
                Buttons = MessageBoxButtons.OkCancel,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Test: shows a message box with Yes/No buttons and Question icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxYesNo()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is a question message with Yes and No buttons. Do you want to proceed?",
                Buttons = MessageBoxButtons.YesNo,
                DefaultButton = MessageBoxButton.Yes,
                Icon = MessageBoxIcon.Question
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Test: shows a message box with Yes/No/Cancel buttons and Error icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxYesNoCancel()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is an error message with Yes, No, and Cancel buttons.",
                Buttons = MessageBoxButtons.YesNoCancel,
                DefaultButton = MessageBoxButton.Yes,
                Icon = MessageBoxIcon.Error
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        #endregion // Debug commands

        // Shows a MessageBox listing overwriting files; returns true if the user confirms.
        private async Task<bool> ConfirmOverwriteAsync(List<string> files)
        {
            if (files.Count == 0)
                return true;
            string fileList = string.Join("\n", files);
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = string.Format(MiscText.OverwriteFilesPrompt, fileList),
                Buttons = MessageBoxButtons.OkCancel,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning,
            };
            await Services.DialogService.ShowDialogAsync(vm);
            return vm.ChosenButton == MessageBoxButton.Ok;
        }

    }
}
