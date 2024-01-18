using System;
using Godot;

namespace OsuTimer.Classes.Visual;

public partial class VolumeSlider : VScrollBar
{
    private int busIndex;
    [Export] public string BusName = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        busIndex = AudioServer.GetBusIndex(BusName);
        ValueChanged += OnValueChanged;

        float invertedValue = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
        Value = Math.Abs(1 - invertedValue);
    }

    public void OnValueChanged(double value)
    {
        double invertedValue = Math.Abs(1 - value);
        AudioServer.SetBusVolumeDb(
            busIndex,
            Mathf.LinearToDb((float)invertedValue));
    }
}