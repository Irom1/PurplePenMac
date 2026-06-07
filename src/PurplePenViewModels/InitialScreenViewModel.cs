// InitialScreenViewModel.cs
//
// ViewModel for the Purple Pen initial/welcome screen shown at startup.
// Offers Create New Event, Open Existing, Open Last, Open Sample options.
//
// Ported from WinForms PurplePen/InitialScreen.cs.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    public enum InitialScreenChoice
    {
        NewEvent,
        OpenExisting,
        OpenLast,
        OpenSample
    }

    public partial class InitialScreenViewModel : ViewModelBase
    {
        [ObservableProperty]
        private InitialScreenChoice selectedChoice = InitialScreenChoice.NewEvent;

        /// <summary>
        /// Whether the "Open Last Event" option is available.
        /// </summary>
        public bool CanOpenLast { get; set; }

        /// <summary>
        /// Display text for the "Open Last Event" radio button.
        /// </summary>
        public string OpenLastText { get; set; } = "";

        /// <summary>
        /// Whether the "Open Sample Event" option is available.
        /// </summary>
        public bool CanOpenSample { get; set; }

        public InitialScreenViewModel()
        {
            // Default to Open Last if available, otherwise New Event
            SelectedChoice = CanOpenLast ? InitialScreenChoice.OpenLast : InitialScreenChoice.NewEvent;
        }
    }
}
