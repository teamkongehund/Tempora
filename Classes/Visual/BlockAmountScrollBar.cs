using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class BlockAmountScrollBar : HScrollBar {
    public Label Label;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Label = GetNode<Label>("Label");

        UpdateLabel(Settings.Instance.NumberOfBlocks);
        Value = Settings.Instance.NumberOfBlocks;

        MaxValue = Settings.Instance.MaxNumberOfBlocks;

        ValueChanged += OnValueChanged;
    }

    public void OnValueChanged(double value) {
        UpdateLabel(value);
    }

    public void UpdateLabel(double value) {
        Label.Text = ((int)value).ToString();
    }
}