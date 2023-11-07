using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Data class controlling how the tempo of the song varies with time.
/// </summary>
public partial class Timing : Node
{
	[Signal] public delegate void TimingChangedEventHandler();

	public List<TimingPoint> TimingPoints = new List<TimingPoint>();

	public AudioFile AudioFile;

	public static Timing Instance;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
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
		TimingPoints.Sort();
	}

	/// <summary>
	/// Add a timing point at a given time, which is 
	/// </summary>
	/// <param name="time"></param>
	public void AddTimingPoint(float time)
	{
		TimingPoint timingPoint = new TimingPoint() 
		{ 
			Time = time 
		};
		TimingPoints.Add(timingPoint);
		AddChild(timingPoint);

        TimingPoints.Sort();

		int index = TimingPoints.FindIndex(point => point == timingPoint);
		
		// Update MusicPosition based on previous Timing Point
		if (index >= 1) 
		{
			TimingPoint previousTimingPoint = TimingPoints[index - 1];
			float timeDifference = timingPoint.Time - previousTimingPoint.Time;
			float musicPositionDifference = previousTimingPoint.MeasuresPerSecond * timeDifference;
			timingPoint.MusicPosition = previousTimingPoint.MusicPosition + musicPositionDifference;

			timingPoint.MeasuresPerSecond = previousTimingPoint.MeasuresPerSecond;
		}
		else if (index == 0)
		{
            TimingPoint nextTimingPoint = TimingPoints.Count > 1 ? TimingPoints[index + 1] : null;
			if (nextTimingPoint == null) 
			{
				// Set MusicPosition based on default timing (120 BPM = 0.5 MPS from Time = 0)
				timingPoint.MusicPosition = 0.5f * time;
			}
			else
			{
				float timeDifference = timingPoint.Time - nextTimingPoint.Time;
				float musicPositionDifference = nextTimingPoint.MeasuresPerSecond * timeDifference;
                timingPoint.MusicPosition = nextTimingPoint.MusicPosition + musicPositionDifference;
            }

        }

		timingPoint.TimingPointChanged += OnTimingPointChanged;

		GD.Print("Current Timing points:");
		foreach (TimingPoint child in TimingPoints)
		{
			//GD.Print("Time " + child.Time + " , MusicPosition " + child.MusicPosition);
			GD.Print($"Time {child.Time}, Music Position {child.MusicPosition}, BPM {child.BPM}");
		}
    }

	public void OnTimingPointChanged(TimingPoint timingPoint)
	{
        int index = TimingPoints.FindIndex(i => i == timingPoint);

		GD.Print($"{Time.GetTicksMsec() / 1e3} - Now updating MPS!");

        UpdateMPSBasedOnNextTimingPoint(index-1);
        UpdateMPSBasedOnNextTimingPoint(index);

        GD.Print($"{Time.GetTicksMsec() / 1e3} - Done updating MPS!");

        EmitSignal(nameof(TimingChanged));
    }

	/// <summary>
	/// Use differences between time and music position to calculate <see cref="TimingPoint.MeasuresPerSecond"/> (BPM analogy)
	/// </summary>
	public void UpdateMPSBasedOnNextTimingPoint(int index)
	{
		// If the current index doesn't exist, don't do anything
		if (index >= TimingPoints.Count || index < 0) return;

		// If the next index doesn't exist and there's no previous timing point, don't do anything
		else if (index + 1 >= TimingPoints.Count && index - 1 < 0 && index < TimingPoints.Count) return;

        // If the next index doesn't exist, but there's a previous timing point, continue the MPS from previous timing point.
        else if (index+1 >= TimingPoints.Count && index - 1 >= 0 && index < TimingPoints.Count)
        {
            TimingPoints[index].MeasuresPerSecond = TimingPoints[index - 1].MeasuresPerSecond;
            return;
        }

		// If the next index exists, use the differences between time and music position to calculate the MPS
        float? timeDifference = GetTimeDifference(index, index+1);
		if (timeDifference == null)
		{
			throw new Exception("GetTimeDifference returned null - Check the index checks in the method that threw this error.");
		}

        float? musicPositionDifference = TimingPoints[index+1].MusicPosition - TimingPoints[index].MusicPosition;
        if (musicPositionDifference == null) throw new Exception("Previous Timing Point did not have a Music Position");

		TimingPoints[index].MeasuresPerSecond = (float)musicPositionDifference/(float)timeDifference;
    }

    public float? GetTimeDifference(int index1, int index2)
    {
		if (index1 < 0 || index2 < 0 || index1 > TimingPoints.Count || index2 > TimingPoints.Count) return null;
        return TimingPoints[index2].Time - TimingPoints[index1].Time;
    }
}
