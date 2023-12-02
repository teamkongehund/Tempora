using Godot;

namespace OsuTimer.Classes.Visual;

public partial class PlaybackRateScrollBar : HScrollBar {
    public Label Label;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Label = GetNode<Label>("Label");
        UpdateLabel();

        ValueChanged += OnValueChanged;
    }

    public void OnValueChanged(double value) {
        UpdateLabel();
    }

    public void UpdateLabel() {
        Label.Text = ((int)(Value * 100)) + " %";
    }
}