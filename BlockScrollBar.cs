using Godot;
using System;

public partial class BlockScrollBar : VScrollBar
{
	Timing Timing;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Timing = Timing.Instance;

		Signals.Instance.TimingChanged += OnTimingChanged;

		//ValueChanged += OnValueChanged;
	}

    public void OnTimingChanged()
	{
		UpdateMaxValue();
	}


    public void UpdateMaxValue()
	{
		// Update min and max value

		int length = Timing.GetLengthInMeasures();

		GD.Print($"Length of song is {length} measures");

		MaxValue = length;
	}

	//public void OnValueChanged(double value)
	//{
	//	GD.Print(value);
	//}
}
