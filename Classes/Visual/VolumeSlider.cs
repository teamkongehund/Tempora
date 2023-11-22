using Godot;
using System;

public partial class VolumeSlider : VScrollBar
{
	[Export] public string BusName;

	private int BusIndex;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BusIndex = AudioServer.GetBusIndex(BusName);
		ValueChanged += OnValueChanged;

		float invertedValue = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(BusIndex));
		Value = Math.Abs(1 - invertedValue);
	}

	public void OnValueChanged(double value)
	{
		double invertedValue = Math.Abs(1 - value);
		AudioServer.SetBusVolumeDb(
			BusIndex,
			Mathf.LinearToDb((float)invertedValue));
	}
}
