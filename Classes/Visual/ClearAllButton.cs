using Godot;
using OsuTimer.Classes.Utility;
using System;

public partial class ClearAllButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Pressed += OnPressed;
	}

    private void OnPressed()
    {
        Timing.Instance.TimingPoints.Clear();
        Timing.Instance.TimeSignaturePoints.Clear();
        Signals.Instance.EmitSignal("TimingChanged");
        ReleaseFocus();
    }
}
