// MoveAllControlsDialogViewModel.cs
//
// ViewModel for the Move All Controls dialog. Lets the user choose
// the type of transformation to apply to all controls.
//
// Ported from WinForms PurplePen/MoveAllControls.cs.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// The selected transformation action for moving all controls.
    /// </summary>
    public enum MoveAllControlsActionChoice
    {
        Move,
        MoveScale,
        MoveRotate,
        MoveRotateScale
    }

    /// <summary>
    /// ViewModel for the Move All Controls action selection dialog.
    /// </summary>
    public partial class MoveAllControlsDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private MoveAllControlsActionChoice selectedAction = MoveAllControlsActionChoice.Move;
    }
}
