// MoveAllControlsDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    public partial class MoveAllControlsDialog : Window
    {
        public MoveAllControlsDialog()
        {
            InitializeComponent();
            Opened += (_, _) => PopulateComboBox();
        }

        private void PopulateComboBox()
        {
            actionComboBox.Items.Clear();
            // Localized strings for these are in UIText.resx under MoveAllControlsDialog_action*
            actionComboBox.Items.Add(new ComboBoxItem {
                Content = "Move Only",
                Tag = MoveAllControlsActionChoice.Move });
            actionComboBox.Items.Add(new ComboBoxItem {
                Content = "Move and Scale",
                Tag = MoveAllControlsActionChoice.MoveScale });
            actionComboBox.Items.Add(new ComboBoxItem {
                Content = "Move and Rotate",
                Tag = MoveAllControlsActionChoice.MoveRotate });
            actionComboBox.Items.Add(new ComboBoxItem {
                Content = "Move, Rotate, and Scale",
                Tag = MoveAllControlsActionChoice.MoveRotateScale });
            actionComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MoveAllControlsDialogViewModel vm &&
                actionComboBox.SelectedItem is ComboBoxItem item &&
                item.Tag is MoveAllControlsActionChoice action) {
                vm.SelectedAction = action;
            }
            Close(true);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
