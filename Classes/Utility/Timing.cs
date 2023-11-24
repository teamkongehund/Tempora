using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Data class controlling how the tempo of the song varies with time.
/// </summary>
public partial class Timing : Node
{
    #region Properties & Signals
    Signals Signals;

	//[Signal] public delegate void TimingChangedEventHandler();

	public List<TimingPoint> TimingPoints = new List<TimingPoint>();

	public List<TimeSignaturePoint> TimeSignaturePoints = new List<TimeSignaturePoint>();

	public AudioFile AudioFile;

	public static Timing Instance;
    #endregion

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Signals = Signals.Instance;
        Instance = this;
	}

    #region Timing modifiers

    public void AddTimingPoint(float MusicPosition, float Time)
	{
		TimingPoint timingPoint = new TimingPoint()
		{
			MusicPosition = MusicPosition,
			Time = Time
		};
		TimingPoints.Add(timingPoint);
		//AddChild(timingPoint);
		timingPoint.Changed += OnTimingPointChanged;
		TimingPoints.Sort();

		//EmitSignal(nameof(TimingChanged));
        Signals.EmitSignal("TimingChanged");
    }

	public void AddTimingPoint(float time)
	{
		TimingPoint timingPoint = null;
		AddTimingPoint(time, out timingPoint);
	}

	/// <summary>
	/// Add a timing point at a given time, which is 
	/// </summary>
	/// <param name="time"></param>
	public void AddTimingPoint(float time, out TimingPoint outTimingPoint)
	{
		TimingPoint timingPoint = new TimingPoint() 
		{ 
			Time = time,
			TimeSignature = GetTimeSignature(time),
		};
		TimingPoints.Add(timingPoint);

        TimingPoints.Sort();

		TimingPoint previousTimingPoint = null;
        TimingPoint nextTimingPoint = null;

        int index = TimingPoints.FindIndex(point => point == timingPoint);
		if (index >= 1) // Set MusicPosition based on previous TimingPoint
        {
            previousTimingPoint = TimingPoints[index - 1];

            timingPoint.MusicPosition = TimeToMusicPosition(time); // TODO 3: verify this doesn't accidentally use itself to get value
            timingPoint.MeasuresPerSecond = previousTimingPoint.MeasuresPerSecond;

			nextTimingPoint = (TimingPoints.Count > index + 1) ? TimingPoints[index + 1] : null;
        }
		else if (index == 0) // Set MusicPosition based on next TimingPoint
		{
            nextTimingPoint = TimingPoints.Count > 1 ? TimingPoints[index + 1] : null;
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

        if (previousTimingPoint?.MusicPosition == timingPoint.MusicPosition
                || nextTimingPoint?.MusicPosition == timingPoint.MusicPosition 
				|| (previousTimingPoint?.Time is float previousTime && Mathf.Abs(previousTime - timingPoint.Time) < 0.04f)
				|| (nextTimingPoint?.Time is float nextTime && Mathf.Abs(nextTime - timingPoint.Time) < 0.04f)
                )
        {
            TimingPoints.Remove(timingPoint);
            outTimingPoint = null;
            return;
        }

		timingPoint.PreviousTimingPoint = previousTimingPoint;
		timingPoint.NextTimingPoint = nextTimingPoint;

        timingPoint.Changed += OnTimingPointChanged;
        timingPoint.Deleted += OnTimingPointDeleted;

		outTimingPoint = timingPoint;

        //EmitSignal(nameof(TimingChanged));
        Signals.EmitSignal("TimingChanged");
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

	/// <summary>
	/// Snap a <see cref="TimingPoint"/> to the grid using <see cref="Settings.Divisor"/> and <see cref="Settings.SnapToGridEnabled"/>
	/// </summary>
	/// <param name="timingPoint"></param>
	/// <param name="musicPosition"></param>
    public static void SnapTimingPoint(TimingPoint timingPoint, float musicPosition)
    {
        if (timingPoint == null)
            return;

		float snappedMusicPosition = SnapMusicPosition(musicPosition);
		timingPoint.MusicPosition = snappedMusicPosition;
    }

	public static float SnapMusicPosition(float musicPosition)
	{
        if (!Settings.Instance.SnapToGridEnabled)
        {
            return musicPosition;
        }

        int divisor = Settings.Instance.Divisor;
        //float divisionLength = 1f / divisor;
		float divisionLength = GetRelativeNotePosition(Timing.Instance.GetTimeSignature(musicPosition), divisor, 1);

        float relativePosition = musicPosition - (int)musicPosition;

        int divisionIndex = (int)Math.Round(relativePosition / divisionLength);

        float snappedMusicPosition = (int)musicPosition + divisionIndex * divisionLength;

		return snappedMusicPosition;
    }

	public void UpdateTimeSignature(int[] timeSignature, int musicPosition)
	{
		int foundPointIndex = TimeSignaturePoints.FindIndex(point => point.MusicPosition == musicPosition);

		TimeSignaturePoint timeSignaturePoint;

        if (foundPointIndex == -1)
		{
			timeSignaturePoint = new TimeSignaturePoint(timeSignature, musicPosition);
			TimeSignaturePoints.Add(timeSignaturePoint);
			TimeSignaturePoints.Sort();
			foundPointIndex = TimeSignaturePoints.FindIndex(point => point.MusicPosition == musicPosition);
        }
		else
		{
			timeSignaturePoint = TimeSignaturePoints[foundPointIndex];
			timeSignaturePoint.TimeSignature = timeSignature;
        }

		if (foundPointIndex > 0 && TimeSignaturePoints[foundPointIndex-1].MusicPosition == timeSignaturePoint.MusicPosition)
		{
			TimeSignaturePoints.Remove(timeSignaturePoint);
			return;
		}

		// Go through all timing points until the next TimeSignaturePoint and update TimeSignature

		int maxIndex = TimingPoints.Count - 1;

        if (foundPointIndex < TimeSignaturePoints.Count - 1)
		{
			int nextMusicPositionWithDifferentTimeSignature = TimeSignaturePoints[foundPointIndex + 1].MusicPosition;
			maxIndex = TimingPoints.FindLastIndex(point => point.MusicPosition < nextMusicPositionWithDifferentTimeSignature);
        }

		int indexForFirstTimingPointWithThisTimeSignature = TimingPoints.FindIndex(point => point.MusicPosition >= musicPosition);

		for (int i = indexForFirstTimingPointWithThisTimeSignature; i <= maxIndex; i++)
		{
			TimingPoint timingPoint = TimingPoints[i];
			TimingPoints[i].TimeSignature = timeSignature;
		}

		Signals.Instance.EmitSignal("TimingChanged");
    }

    #endregion

    #region Calculators
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
    public float? GetTimeDifference(int timingPointIndex1, int timingPointIndex2)
    {
		if (timingPointIndex1 < 0 || timingPointIndex2 < 0 || timingPointIndex1 > TimingPoints.Count || timingPointIndex2 > TimingPoints.Count) return null;
        return TimingPoints[timingPointIndex2].Time - TimingPoints[timingPointIndex1].Time;
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
		//TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
		//if (timingPoint == null) return new int[] { 4, 4 };
		//else return timingPoint.TimeSignature;

		TimeSignaturePoint timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.MusicPosition <= musicPosition);
		if (timeSignaturePoint == null)
			return new int[] { 4, 4 };

		return timeSignaturePoint.TimeSignature;
    }
	/// <summary>
	/// Returns the music position of the beat at or right before the given music position.
	/// </summary>
	/// <param name="musicPosition"></param>
	/// <returns></returns>
	public float GetBeatPosition(float musicPosition)
	{
		int[] timeSignature = GetTimeSignature(musicPosition);
		float beatIncrement = (timeSignature[1] / 4f) / timeSignature[0];
		float relativePosition = musicPosition % 1;
		float position = (int)(relativePosition / beatIncrement) * beatIncrement + (int)musicPosition;
		return position;
	}
	public static float GetRelativeNotePosition(int[] timeSignature, int gridDivisor, int index)
	{
        // For a quarter-note:
        // 4/4: 0, 1/4, 2/4, 3/4
        // 3/4: 0, 1/3, 2/3
        // 7/8: 0, 2/7, 4/7, 6/7

        // For a (1/12) note:
        // 4/4: 0, 1/12, 2/12, etc.
        // 3/4: 0, 1/9, 2/9, 3/9
        // 7/4: 0, 1/21, 2/21, 3/21, etc.
        // 7/8: 0, 2/21, 4/21, 6/21, etc.

        float position = index * timeSignature[1] / (float)(timeSignature[0] * gridDivisor);
        return position;
    }
	public int GetLastMeasure()
	{
		float lengthInSeconds = AudioFile.SampleIndexToSeconds(AudioFile.AudioData.Length - 1);
		float lastMeasure = TimeToMusicPosition(lengthInSeconds);
		return (int)lastMeasure;
	}
    #endregion
}
