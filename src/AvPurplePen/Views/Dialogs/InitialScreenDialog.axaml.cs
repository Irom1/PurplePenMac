// InitialScreenDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    public partial class InitialScreenDialog : Window
    {
        public InitialScreenDialog()
        {
            InitializeComponent();
            Opened += (_, _) =>
            {
                // Set the open-last radio button text
                if (DataContext is InitialScreenViewModel vm) {
                    if (vm.CanOpenLast) {
                        openLastRadio.Content = vm.OpenLastText;
                        openLastRadio.IsVisible = true;
                        newEventRadio.IsChecked = false;
                        openLastRadio.IsChecked = true;
                    }
                    else {
                        openLastRadio.IsVisible = false;
                        newEventRadio.IsChecked = true;
                    }

                    if (!vm.CanOpenSample) {
                        openSampleRadio.IsVisible = false;
                    }

                    // Focus the OK button
                    okButton.Focus();
                }
            };
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is InitialScreenViewModel vm) {
                if (newEventRadio.IsChecked == true)
                    vm.SelectedChoice = InitialScreenChoice.NewEvent;
                else if (openExistingRadio.IsChecked == true)
                    vm.SelectedChoice = InitialScreenChoice.OpenExisting;
                else if (openLastRadio.IsChecked == true)
                    vm.SelectedChoice = InitialScreenChoice.OpenLast;
                else if (openSampleRadio.IsChecked == true)
                    vm.SelectedChoice = InitialScreenChoice.OpenSample;
            }
            Close(true);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
