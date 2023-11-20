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
	}

    public void OnTimingChanged()
	{
		UpdateMaxValue();
	}


    public void UpdateMaxValue()
	{
		int length = Timing.GetLengthInMeasures();
		MaxValue = length;
	}
}
