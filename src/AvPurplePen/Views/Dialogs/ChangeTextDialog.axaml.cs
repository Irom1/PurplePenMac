// ChangeTextDialog.axaml.cs
using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.MapModel;
using PurplePen.ViewModels;
using SkiaSharp;

namespace AvPurplePen.Views
{
    public partial class ChangeTextDialog : Window
    {
        public ChangeTextDialog()
        {
            InitializeComponent();
            Opened += (_, _) =>
            {
                PopulateFontList();
                PopulateColorComboBox();
                textBoxMain.Focus();
            };
        }

        /// <summary>
        /// Populates the font list with system fonts using SkiaSharp.
        /// </summary>
        private void PopulateFontList()
        {
            if (DataContext is ChangeTextDialogViewModel vm) {
                vm.AvailableFonts.Clear();
                try {
                    var families = SKFontManager.Default.GetFontFamilies();
                    foreach (var family in families.OrderBy(f => f)) {
                        vm.AvailableFonts.Add(family);
                    }
                }
                catch {
                    // Fallback: minimal set
                    vm.AvailableFonts.Add("Arial");
                    vm.AvailableFonts.Add("Helvetica");
                    vm.AvailableFonts.Add("Times New Roman");
                    vm.AvailableFonts.Add("Courier New");
                }
            }
        }

        /// <summary>
        /// Populates the color ComboBox with localized names from MiscText.
        /// </summary>
        private void PopulateColorComboBox()
        {
            comboBoxColor.Items.Clear();
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.Black });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.Purple });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.LowerPurple });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.Red });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.Yellow });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.Green });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.LightBlue });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.DarkBlue });
            comboBoxColor.Items.Add(new ComboBoxItem { Content = PurplePen.MiscText.CustomColor });

            // Sync ComboBox selection with ViewModel's SelectedColorIndex
            if (DataContext is ChangeTextDialogViewModel vm) {
                comboBoxColor.SelectedIndex = vm.SelectedColorIndex;
            }
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e) => Close(true);

        private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(false);

        /// <summary>
        /// Shows a context menu with macro insertion options.
        /// </summary>
        private void InsertSpecialButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ChangeTextDialogViewModel vm || !vm.ShowInsertSpecial)
                return;

            var flyout = new Flyout();

            var menu = new MenuFlyout();
            AddMacroItem(menu, "Event Title", vm.InsertEventTitleCommand);
            AddMacroItem(menu, "Course Name", vm.InsertCourseNameCommand);
            AddMacroItem(menu, "Course Part", vm.InsertCoursePartCommand);
            AddMacroItem(menu, "Course Length", vm.InsertCourseLengthCommand);
            AddMacroItem(menu, "Course Climb", vm.InsertCourseClimbCommand);
            AddMacroItem(menu, "Class List", vm.InsertClassListCommand);
            AddMacroItem(menu, "Print Scale", vm.InsertPrintScaleCommand);
            AddMacroItem(menu, "Variation", vm.InsertVariationCommand);
            AddMacroItem(menu, "Relay Team", vm.InsertRelayTeamCommand);
            AddMacroItem(menu, "Relay Leg", vm.InsertRelayLegCommand);
            AddMacroItem(menu, "File Name", vm.InsertFileNameCommand);
            AddMacroItem(menu, "Map File Name", vm.InsertMapFileNameCommand);

            insertSpecialButton.ContextFlyout = menu;
            menu.ShowAt(insertSpecialButton);
        }

        private static void AddMacroItem(MenuFlyout menu, string header, System.Windows.Input.ICommand command)
        {
            menu.Items.Add(new MenuItem {
                Header = header,
                Command = command
            });
        }
    }
}
