// OverwritingOcadFilesDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    public partial class OverwritingOcadFilesDialog : Window
    {
        public OverwritingOcadFilesDialog()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object? sender, RoutedEventArgs e) => Close(true);

        private void ButtonCancel_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
