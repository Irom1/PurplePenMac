// ChangeTextDialogViewModel.cs
//
// ViewModel for the Add/Edit Text dialog. Used for both adding new text
// specials and editing existing text specials on the map.
// Provides text entry, font selection, bold/italic, font size, and color
// selection (presets + custom CMYK).
//
// Ported from WinForms PurplePen/ChangeText.cs.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PurplePen.Graphics2D;
using PurplePen.MapModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// Represents a preset color choice in the SpecialColorChooser.
    /// The View populates the ComboBox with localized names.
    /// </summary>
    public class SpecialColorItem
    {
        /// <summary>
        /// The SpecialColor for this item, or null for Custom.
        /// </summary>
        public SpecialColor? SpecialColor { get; set; }

        /// <summary>
        /// The CMYK color for this item, or null to use the purple color.
        /// </summary>
        public CmykColor? CmykColor { get; set; }
    }

    /// <summary>
    /// ViewModel for the Change Text dialog.
    /// </summary>
    public partial class ChangeTextDialogViewModel : ViewModelBase
    {
        // ── Dialog identity ──────────────────────────────────────────────────

        /// <summary>
        /// The dialog title bar text.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// The explanation text shown above the text box.
        /// </summary>
        public string Explanation { get; set; } = "";

        // ── Text content ─────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUserTextValid))]
        private string userText = "";

        /// <summary>
        /// True when the text box is non-empty — enables OK button.
        /// </summary>
        public bool IsUserTextValid => !string.IsNullOrEmpty(UserText);

        // ── Font properties ──────────────────────────────────────────────────

        /// <summary>
        /// The selected font family name (e.g. "Arial").
        /// </summary>
        public string FontName { get; set; } = "Arial";

        /// <summary>
        /// List of available font family names, populated by the View.
        /// </summary>
        public System.Collections.Generic.List<string> AvailableFonts { get; } = new();

        [ObservableProperty]
        private bool fontBold;

        [ObservableProperty]
        private bool fontItalic;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FontSizeEnabled))]
        private decimal fontSize = 5.0m;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FontSizeEnabled))]
        private bool fontSizeAutomatic = true;

        /// <summary>
        /// Whether the font size NumericUpDown is enabled.
        /// </summary>
        public bool FontSizeEnabled => !FontSizeAutomatic;

        // ── Color selection ──────────────────────────────────────────────────

        /// <summary>
        /// The list of preset color choices. The View populates the ComboBox
        /// with localized text for each index.
        /// </summary>
        public System.Collections.Generic.List<SpecialColorItem> ColorChoices { get; } = new();

        /// <summary>
        /// The purple color from the map, used for "Purple" entries.
        /// </summary>
        public CmykColor PurpleColor { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomColor))]
        private int selectedColorIndex;

        /// <summary>
        /// True when the "Custom Color" item (last in list) is selected.
        /// </summary>
        public bool IsCustomColor => SelectedColorIndex == ColorChoices.Count - 1 && ColorChoices.Count > 0;

        /// <summary>
        /// CMYK values for the custom color (0-100 scale).
        /// </summary>
        [ObservableProperty] private decimal customCyan;
        [ObservableProperty] private decimal customMagenta;
        [ObservableProperty] private decimal customYellow;
        [ObservableProperty] private decimal customBlack;

        // ── Insert Special macros ────────────────────────────────────────────

        /// <summary>
        /// Whether the "Insert Special" button is visible.
        /// </summary>
        public bool ShowInsertSpecial { get; set; } = true;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Gets the currently selected SpecialColor.
        /// </summary>
        public SpecialColor GetSelectedColor()
        {
            if (SelectedColorIndex >= 0 && SelectedColorIndex < ColorChoices.Count - 1) {
                var item = ColorChoices[SelectedColorIndex];
                if (item.SpecialColor != null)
                    return item.SpecialColor;
            }
            // Custom color
            return new SpecialColor(GetCmykColor());
        }

        /// <summary>
        /// Gets the CMYK color for the current selection.
        /// </summary>
        public CmykColor GetCmykColor()
        {
            if (IsCustomColor) {
                return CmykColor.FromCmyk(
                    (float)(CustomCyan / 100m),
                    (float)(CustomMagenta / 100m),
                    (float)(CustomYellow / 100m),
                    (float)(CustomBlack / 100m));
            }
            if (SelectedColorIndex >= 0 && SelectedColorIndex < ColorChoices.Count) {
                var item = ColorChoices[SelectedColorIndex];
                if (item.CmykColor != null)
                    return item.CmykColor;
                return PurpleColor;
            }
            return CmykColor.FromCmyk(0, 0, 0, 1); // default black
        }

        /// <summary>
        /// Initializes the color choices with preset colors.
        /// </summary>
        public void InitializeColors(CmykColor purpleColor)
        {
            PurpleColor = purpleColor;
            ColorChoices.Clear();
            // 0: Black
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = SpecialColor.Black,
                CmykColor = CmykColor.FromCmyk(0, 0, 0, 1)
            });
            // 1: Purple
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = SpecialColor.UpperPurple,
                CmykColor = purpleColor
            });
            // 2: Lower Purple
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = SpecialColor.LowerPurple,
                CmykColor = purpleColor
            });
            // 3: Red
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = new SpecialColor(CmykColor.FromCmyk(0, 1, 1, 0)),
                CmykColor = CmykColor.FromCmyk(0, 1, 1, 0)
            });
            // 4: Yellow
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = new SpecialColor(CmykColor.FromCmyk(0, 0, 1, 0)),
                CmykColor = CmykColor.FromCmyk(0, 0, 1, 0)
            });
            // 5: Green
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = new SpecialColor(CmykColor.FromCmyk(1, 0, 1, 0)),
                CmykColor = CmykColor.FromCmyk(1, 0, 1, 0)
            });
            // 6: Light Blue
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = new SpecialColor(CmykColor.FromCmyk(1, 0, 0, 0)),
                CmykColor = CmykColor.FromCmyk(1, 0, 0, 0)
            });
            // 7: Dark Blue
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = new SpecialColor(CmykColor.FromCmyk(1, 1, 0, 0)),
                CmykColor = CmykColor.FromCmyk(1, 1, 0, 0)
            });
            // 8: Custom Color (last — IsCustomColor checks for this position)
            ColorChoices.Add(new SpecialColorItem {
                SpecialColor = null,
                CmykColor = null
            });
        }

        /// <summary>
        /// Sets the selected color from a SpecialColor.
        /// </summary>
        public void SetSelectedColor(SpecialColor color)
        {
            for (int i = 0; i < ColorChoices.Count - 1; i++) {
                var item = ColorChoices[i];
                if (item.SpecialColor != null && item.SpecialColor.Equals(color)) {
                    SelectedColorIndex = i;
                    return;
                }
            }
            // Custom color
            CmykColor cmyk = color.CustomColor ?? CmykColor.FromCmyk(0, 0, 0, 1);
            CustomCyan = (decimal)(cmyk.Cyan * 100);
            CustomMagenta = (decimal)(cmyk.Magenta * 100);
            CustomYellow = (decimal)(cmyk.Yellow * 100);
            CustomBlack = (decimal)(cmyk.Black * 100);
            SelectedColorIndex = ColorChoices.Count - 1;
        }

        // ── Text macro insertion commands ────────────────────────────────────

        [RelayCommand] private void InsertEventTitle() => AppendMacro(TextMacros.EventTitle);
        [RelayCommand] private void InsertCourseName() => AppendMacro(TextMacros.CourseName);
        [RelayCommand] private void InsertCoursePart() => AppendMacro(TextMacros.CoursePart);
        [RelayCommand] private void InsertCourseLength() => AppendMacro(TextMacros.CourseLength);
        [RelayCommand] private void InsertCourseClimb() => AppendMacro(TextMacros.CourseClimb);
        [RelayCommand] private void InsertClassList() => AppendMacro(TextMacros.ClassList);
        [RelayCommand] private void InsertPrintScale() => AppendMacro(TextMacros.PrintScale);
        [RelayCommand] private void InsertVariation() => AppendMacro(TextMacros.Variation);
        [RelayCommand] private void InsertRelayTeam() => AppendMacro(TextMacros.RelayTeam);
        [RelayCommand] private void InsertRelayLeg() => AppendMacro(TextMacros.RelayLeg);
        [RelayCommand] private void InsertFileName() => AppendMacro(TextMacros.FileName);
        [RelayCommand] private void InsertMapFileName() => AppendMacro(TextMacros.MapFileName);

        private void AppendMacro(string macro)
        {
            // Append macro to the text content.
            UserText += macro;
        }
    }
}
