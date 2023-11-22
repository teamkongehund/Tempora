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
		UpdateRange();
	}

    public void UpdateRange()
	{
		int firstMeasure = (int)Timing.TimeToMusicPosition(0) - 1;
		int lastMeasure = Timing.GetLastMeasure() - (Settings.Instance.NumberOfBlocks - 1);
		MinValue = firstMeasure;
		MaxValue = lastMeasure;
	}
}
