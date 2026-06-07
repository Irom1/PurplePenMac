// LicenseDialog.axaml.cs
//
// Code-behind for the Purple Pen license dialog. Displays the BSD license
// text and provides a link to the BSD License Wikipedia page.
//
// Migrated from WinForms PurplePen/LicenseForm.cs.

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Read-only dialog that displays the Purple Pen BSD license text.
    /// No DataContext or ViewModel required.
    /// </summary>
    public partial class LicenseDialog : Window
    {
        // Plain-text BSD license; converted from the RTF content in WinForms LicenseForm.cs.
        private const string LicenseText =
            "Copyright © 2007-2012, Peter Golde\r\n" +
            "All rights reserved.\r\n\r\n" +
            "Redistribution and use in source and binary forms, with or without " +
            "modification, are permitted provided that the following conditions are met:\r\n\r\n" +
            "•  Redistributions of source code must retain the above copyright notice, " +
            "this list of conditions and the following disclaimer.\r\n\r\n" +
            "•  Redistributions in binary form must reproduce the above copyright notice, " +
            "this list of conditions and the following disclaimer in the documentation " +
            "and/or other materials provided with the distribution.\r\n\r\n" +
            "•  Neither the name of Peter Golde, nor \"Purple Pen\", nor the names of its " +
            "contributors may be used to endorse or promote products derived from this " +
            "software without specific prior written permission.\r\n\r\n" +
            "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" " +
            "AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE " +
            "IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE " +
            "DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR " +
            "ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES " +
            "(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; " +
            "LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON " +
            "ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT " +
            "(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS " +
            "SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";

        /// <summary>
        /// Initializes the dialog and populates the license text.
        /// </summary>
        public LicenseDialog()
        {
            InitializeComponent();
            licenseTextBlock.Text = LicenseText;
        }

        /// <summary>
        /// Opens the BSD License Wikipedia article in the default browser.
        /// </summary>
        private void BsdLicenseLinkButton_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://en.wikipedia.org/wiki/BSD_License",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
