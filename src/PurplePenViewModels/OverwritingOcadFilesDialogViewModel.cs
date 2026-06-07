// OverwritingOcadFilesDialogViewModel.cs
//
// ViewModel for the dialog that confirms overwriting existing files during
// export operations (OCAD, RouteGadget, etc.). Displays the list of files
// that will be overwritten with OK/Cancel buttons.
//
// Ported from WinForms PurplePen/OverwritingOcadFilesDialog.cs.

using System.Collections.ObjectModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Confirm Replace Files" dialog shown before overwriting
    /// existing export files.
    /// </summary>
    public class OverwritingOcadFilesDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The warning/instruction text shown above the file list.
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// The list of file paths that will be overwritten.
        /// </summary>
        public ObservableCollection<string> Filenames { get; } = new ObservableCollection<string>();
    }
}
