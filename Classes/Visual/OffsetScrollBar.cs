using Godot;
using System;

public partial class OffsetScrollBar : HScrollBar
{
    public Label Label;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Label = GetNode<Label>("Label");

        UpdateLabel(Settings.Instance.MusicPositionOffset);
        Value = Settings.Instance.MusicPositionOffset;

        ValueChanged += OnValueChanged;
    }

    public void OnValueChanged(double value)
    {
        UpdateLabel(value);
    }

    public void UpdateLabel(double value) => Label.Text = (value * 100).ToString("0") + " %";
}