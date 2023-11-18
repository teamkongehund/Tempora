using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Data class controlling how the tempo of the song varies with time.
/// </summary>
public partial class Timing : Node
{
	Signals Signals;

	[Signal] public delegate void TimingChangedEventHandler();

	public List<TimingPoint> TimingPoints = new List<TimingPoint>();

	public AudioFile AudioFile;

	public static Timing Instance;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Signals = Signals.Instance;
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
		timingPoint.Changed += OnTimingPointChanged;
		TimingPoints.Sort();

		//EmitSignal(nameof(TimingChanged));
        Signals.EmitSignal("TimingChanged");
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
		
		// Set MusicPosition based on previous Timing Point
		if (index >= 1) 
		{
			TimingPoint previousTimingPoint = TimingPoints[index - 1];

			//float timeDifference = timingPoint.Time - previousTimingPoint.Time;
			//float musicPositionDifference = previousTimingPoint.MeasuresPerSecond * timeDifference;
			//timingPoint.MusicPosition = previousTimingPoint.MusicPosition + musicPositionDifference;
			timingPoint.MusicPosition = TimeToMusicPosition(time);
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

		timingPoint.Changed += OnTimingPointChanged;
        timingPoint.Deleted += OnTimingPointDeleted;

        //EmitSignal(nameof(TimingChanged));
        Signals.EmitSignal("TimingChanged");
    }

	public void PrintTimingPoints()
	{
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

        UpdateMPSBasedOnNextTimingPoint(index-1);
        UpdateMPSBasedOnNextTimingPoint(index);

        Signals.EmitSignal("TimingChanged");
    }

	public void OnTimingPointDeleted(TimingPoint timingPoint)
	{
        int index = TimingPoints.FindIndex(i => i == timingPoint);

        timingPoint.QueueFree();
        TimingPoints.Remove(timingPoint);

        UpdateMPSBasedOnNextTimingPoint(index - 1);
        UpdateMPSBasedOnNextTimingPoint(index);

        Signals.EmitSignal("TimingChanged");
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

	/// <summary>
	/// Returns the <see cref="TimingPoint"/> at or right before a given music position. If none exist, returns the first one after the music position.
	/// </summary>
	/// <param name="musicPosition"></param>
	/// <returns></returns>
	public TimingPoint GetOperatingTimingPoint(float musicPosition)
	{
        if (TimingPoints.Count == 0) return null;

        TimingPoint timingPoint = TimingPoints.FindLast(point => point.MusicPosition <= musicPosition);

        // If there's only TimingPoints AFTER MusicPositionStart
        if (timingPoint == null)
            timingPoint = TimingPoints.Find(point => point.MusicPosition > musicPosition);

		return timingPoint;
    }

	public float MusicPositionToTime(float musicPosition)
	{
		TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
		if (timingPoint == null) return musicPosition / 0.5f; // default 120 bpm from time=0

        float time = (float)(timingPoint.Time + (musicPosition - timingPoint.MusicPosition) / timingPoint.MeasuresPerSecond);

		return time;
    }

	public float TimeToMusicPosition(float time)
	{
		if (TimingPoints.Count == 0) return time * 0.5f; // default 120 bpm from time=0

        int timingPointIndex = TimingPoints.FindLastIndex(point => point.Time <= time);
		TimingPoint timingPoint;

        if (timingPointIndex == -1) // If there's no TimingPoint behind - find first TimingPoint after.	
            timingPoint = TimingPoints.Find(point => point.Time > time);
		else
			timingPoint = TimingPoints[timingPointIndex];

		if (timingPoint.MusicPosition == null && timingPointIndex > 0)
			timingPoint = TimingPoints[timingPointIndex - 1];


		return (float)((time - timingPoint.Time) * timingPoint.MeasuresPerSecond + timingPoint.MusicPosition);

		// creating new Timing points with <= picks itself, tho there's no music position
	}


	public int[] GetTimeSignature(float musicPosition)
	{
		TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
		if (timingPoint == null) return new int[] { 4, 4 };
		else return timingPoint.TimeSignature;
    }

	/// <summary>
	/// Returns the music position of the beat at or right before the given music position.
	/// </summary>
	/// <param name="musicPosition"></param>
	/// <returns></returns>
	public float GetBeatPosition(float musicPosition)
	{
		int[] timeSignature = GetTimeSignature(musicPosition);

		// 4/4: 0, 1/4, 2/4, 3/4
		// 3/4: 0, 1/3, 2/3
		// 7/8: 0, 2/7, 4/7, 6/7
		// Divide by upper number
		// Multiply by lower number / 4

		float beatIncrement = (timeSignature[1] / 4f) / timeSignature[0];
		float relativePosition = musicPosition % 1;
		float position = (int)(relativePosition / beatIncrement) * beatIncrement + (int)musicPosition;
		return position;
	}

	public float GetNotePosition(float musicPosition, int gridDivisor)
	{
        int[] timeSignature = GetTimeSignature(musicPosition);

        float beatIncrement = (timeSignature[1] / (float) gridDivisor) / timeSignature[0];
        float relativePosition = musicPosition % 1;
        float position = (int)(relativePosition / beatIncrement) * beatIncrement + (int)musicPosition;
        return position;
    }

	public static float GetRelativeNotePosition(int[] timeSignature, int gridDivisor, int index)
	{
        float beatIncrement = (timeSignature[1] / (float)gridDivisor) / timeSignature[0];
		float position = (int)(index / beatIncrement) * beatIncrement;
        return position;
    }
}
