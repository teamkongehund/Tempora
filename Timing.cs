using Godot;
using System;
using System.Collections.Generic;

public partial class Timing : Node
{
	public List<TimingPoint> TimingPoints = new List<TimingPoint>();

	public AudioFile AudioFile;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void AddTimingPoint(float MusicPosition, float Time)
	{
		TimingPoint timingPoint = new TimingPoint()
		{
			MusicPosition = MusicPosition,
			Time = Time
		};
		TimingPoints.Add(timingPoint);
		AddChild(timingPoint);
		timingPoint.TimingPointChanged += OnTimingPointChanged;
	}

	public void AddTimingPoint(float Time)
	{
		TimingPoint timingPoint = new TimingPoint() 
		{ 
			Time = Time 
		};
		TimingPoints.Add(timingPoint);
		AddChild(timingPoint);

        TimingPoints.Sort();

		int index = TimingPoints.FindIndex(i => i == timingPoint);
		
		// Update MusicPosition based on previous Timing Point
		if (index >= 1) 
		{
			TimingPoint previousTimingPoint = TimingPoints[index - 1];
			float timeDifference = timingPoint.Time - previousTimingPoint.Time;
			float MusicPositionDifference = previousTimingPoint.MeasuresPerSecond * timeDifference;
			timingPoint.MusicPosition = previousTimingPoint.MusicPosition + MusicPositionDifference;
		}

		timingPoint.TimingPointChanged += OnTimingPointChanged;

		foreach (TimingPoint child in TimingPoints)
		{
			GD.Print(child.Time + " " + child.MusicPosition);
		}
    }

	public void OnTimingPointChanged(TimingPoint timingPoint)
	{
		// Update BPM for nearby timing points

		// Forward signal to parent, so visuals can be updated.
	}
}
