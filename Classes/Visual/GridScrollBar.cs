using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class GridScrollBar : HScrollBar {
    public Label Label;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Label = GetNode<Label>("Label");

        Signals.Instance.SettingsChanged += OnSettingsChanged;

        Value = Settings.DivisorToSlider(Settings.Instance.Divisor);

        UpdateLabel();
    }

    public void OnSettingsChanged() {
        UpdateLabel();
    }

    public void UpdateLabel() {
        Label.Text = Settings.Instance.Divisor.ToString();
    }

    // TODO 3: Implement Timer, such that Label inside slider says "Grid",
    // and only displays number for a duration when changing grid
}