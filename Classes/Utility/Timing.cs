using System;
using System.Collections.Generic;
using Godot;

namespace OsuTimer.Classes.Utility;

/// <summary>
///     Data class controlling how the tempo of the song varies with time.
/// </summary>
public partial class Timing : Node
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Instance = this;

    #region Properties & Signals

    private Signals signals = null!;

    private bool isInstantiating;

    public bool IsInstantiating
    {
        get => isInstantiating;
        set
        {
            if (isInstantiating == value)
                return;
            isInstantiating = value;
            //if (value == false)
            //{
            //	Signals.Instance.EmitSignal("TimingChanged");
            //}
        }
    }

    //[Signal] public delegate void TimingChangedEventHandler();

    private List<TimingPoint> timingPoints = [];

    public List<TimingPoint> TimingPoints
    {
        get => timingPoints;
        set
        {
            if (timingPoints == value)
                return;
            timingPoints = value;
            _ = Signals.Instance.EmitSignal("TimingChanged");
        }
    }

    private List<TimeSignaturePoint> timeSignaturePoints = [];

    public List<TimeSignaturePoint> TimeSignaturePoints
    {
        get => timeSignaturePoints;
        set
        {
            if (timeSignaturePoints == value)
                return;
            timeSignaturePoints = value;
            _ = Signals.Instance.EmitSignal("TimingChanged");
        }
    }

    //public AudioFile AudioFile;

    public static Timing Instance = null!;

    #endregion

    #region Timing modifiers

    private void ConnectToTimingPointEvents(TimingPoint timingPoint)
    {
        if (timingPoint == null)
            throw new NullReferenceException($"{nameof(timingPoint)} was null");
        timingPoint.TimeChanged += OnTimingPointTimeChanged;
        timingPoint.BpmChanged += OnTimingPointBpmChanged;
        timingPoint.MusicPositionChanged += OnTimingPointMusicPositionChanged;
        timingPoint.UpdateMeasuresPerSecond += OnTimingPointUpdateMeasuresPerSecond;
        timingPoint.Changed += OnTimingPointChanged;
        timingPoint.Deleted += OnTimingPointDeleted;
    }

    public void AddTimingPoint(float musicPosition, float time)
    {
        var timingPoint = new TimingPoint
        {
            MusicPosition = musicPosition,
            Time = time,
            TimeSignature = GetTimeSignature(musicPosition)
        };
        TimingPoints.Add(timingPoint);
        ConnectToTimingPointEvents(timingPoint);
        TimingPoints.Sort();

        int index = TimingPoints.IndexOf(timingPoint);

        if (index >= 1) // Set previous timing point
        {
            //timingPoint.PreviousTimingPoint = TimingPoints[index - 1];

            if (!IsInstantiating)
                _ = Signals.Instance.EmitSignal("TimingChanged");
        }
    }

    public void AddTimingPoint(float musicPosition, float time, float measuresPerSecond)
    {
        var timingPoint = new TimingPoint
        {
            MusicPosition = musicPosition,
            Time = time,
            TimeSignature = GetTimeSignature(musicPosition)
        };
        TimingPoints.Add(timingPoint);
        ConnectToTimingPointEvents(timingPoint);
        TimingPoints.Sort();

        int index = TimingPoints.IndexOf(timingPoint);

        if (index >= 1) // Set previous timing point
                        //timingPoint.PreviousTimingPoint = TimingPoints[index - 1];

            timingPoint.MeasuresPerSecond = measuresPerSecond;

        if (!IsInstantiating)
            _ = Signals.Instance.EmitSignal("TimingChanged");
    }

    //public void AddTimingPoint(float time) {
    //    TimingPoint timingPoint = null;
    //    AddTimingPoint(time, out timingPoint);
    //}

    /// <summary>
    ///     Add a timing point at a given time, which is
    /// </summary>
    /// <param name="time"></param>
    public void AddTimingPoint(float time, out TimingPoint? outTimingPoint)
    {
        var timingPoint = new TimingPoint
        {
            Time = time,
            TimeSignature = GetTimeSignature(TimeToMusicPosition(time))
        };
        TimingPoints.Add(timingPoint);

        TimingPoints.Sort();

        ConnectToTimingPointEvents(timingPoint);

        TimingPoint previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint nextTimingPoint = GetNextTimingPoint(timingPoint);

        int index = TimingPoints.IndexOf(timingPoint);
        if (index >= 1) // Set MusicPosition based on previous TimingPoint
        {
            //previousTimingPoint = TimingPoints[index - 1];

            timingPoint.MusicPosition = TimeToMusicPosition(time);

            //nextTimingPoint = TimingPoints.Count > index + 1 ? TimingPoints[index + 1] : null;
        }
        else if (index == 0) // Set MusicPosition based on next TimingPoint
        {
            //nextTimingPoint = TimingPoints.Count > 1 ? TimingPoints[index + 1] : null;
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
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"Failed to assign a non-null value to {nameof(timingPoint.MusicPosition)}");

        if (previousTimingPoint.MusicPosition == timingPoint.MusicPosition
            || nextTimingPoint?.MusicPosition == timingPoint.MusicPosition
            || (previousTimingPoint?.MusicPosition is float previousMusicPosition && Mathf.Abs(previousMusicPosition - (float)timingPoint.MusicPosition) < 0.015f)
            || (nextTimingPoint?.MusicPosition is float nextMusicPosition && Mathf.Abs(nextMusicPosition - (float)timingPoint.MusicPosition) < 0.015f)
           )
        {
            _ = TimingPoints.Remove(timingPoint);
            outTimingPoint = null;
            //GD.Print("Timing Point refused to add!");
            return;
        }

        //timingPoint.PreviousTimingPoint = previousTimingPoint;
        //timingPoint.NextTimingPoint = nextTimingPoint;

        outTimingPoint = timingPoint;

        //EmitSignal(nameof(TimingChanged));
        _ = Signals.Instance.EmitSignal("TimingChanged");
    }

    public void OnTimingPointChanged(TimingPoint timingPoint)
    {
        _ = TimingPoints.IndexOf(timingPoint);

        //UpdateMPS(index-1);
        //UpdateMPS(index);

        _ = Signals.Instance.EmitSignal("TimingChanged");
    }

    public void OnTimingPointDeleted(TimingPoint timingPoint)
    {
        _ = TimingPoints.IndexOf(timingPoint);

        timingPoint.QueueFree();
        _ = TimingPoints.Remove(timingPoint);

        //UpdateMPS(index - 1);
        //UpdateMPS(index);

        _ = Signals.Instance.EmitSignal("TimingChanged");
    }

    private void OnTimingPointTimeChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        TimingPoint previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint nextTimingPoint = GetNextTimingPoint(timingPoint);

        // validity checks
        if (previousTimingPoint != null && previousTimingPoint.Time >= timingPoint.NewTime)
            return;
        if (nextTimingPoint != null && nextTimingPoint.Time <= timingPoint.NewTime)
            return;

        timingPoint.IsNewTimeValid = true;
        timingPoint.Time = timingPoint.NewTime;
    }

    private void OnTimingPointBpmChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        TimingPoint nextTimingPoint = GetNextTimingPoint(timingPoint);

        // validity check
        if (nextTimingPoint != null)
            return;

        timingPoint.IsNewBpmValid = true;
        timingPoint.Bpm = timingPoint.NewBpm;
    }

    private void OnTimingPointMusicPositionChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");

        TimingPoint previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint nextTimingPoint = GetNextTimingPoint(timingPoint);

        // validity checks
        if (previousTimingPoint != null && previousTimingPoint.MusicPosition >= timingPoint.NewMusicPosition)
            return;
        if (nextTimingPoint != null && nextTimingPoint.MusicPosition <= timingPoint.NewMusicPosition)
            return;

        timingPoint.IsNewMusicPositionValid = true;
        timingPoint.MusicPosition = timingPoint.NewMusicPosition;
    }

    private void OnTimingPointUpdateMeasuresPerSecond(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"Request to update {nameof(timingPoint.MeasuresPerSecond)} failed because {nameof(timingPoint.MusicPosition)} is null");

        TimingPoint previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint nextTimingPoint = GetNextTimingPoint(timingPoint);

        if (previousTimingPoint?.MusicPosition != null)
        {
            previousTimingPoint.MeasuresPerSecond =
                ((float)timingPoint.MusicPosition - (float)previousTimingPoint.MusicPosition)
                / (timingPoint.Time - previousTimingPoint.Time);
            timingPoint.MeasuresPerSecond = previousTimingPoint.MeasuresPerSecond;

            _ = previousTimingPoint.EmitSignal("Changed", previousTimingPoint);
        }

        if (nextTimingPoint?.MusicPosition != null)
        {
            timingPoint.MeasuresPerSecond =
                ((float)nextTimingPoint.MusicPosition - (float)timingPoint.MusicPosition)
                / (nextTimingPoint.Time - timingPoint.Time);
        }

        _ = timingPoint.EmitSignal("Changed", timingPoint);

    }

    /// <summary>
    ///     Snap a <see cref="TimingPoint" /> to the grid using <see cref="Settings.Divisor" /> and
    ///     <see cref="Settings.SnapToGridEnabled" />
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
            return musicPosition;

        int divisor = Settings.Instance.Divisor;
        //float divisionLength = 1f / divisor;
        float divisionLength = GetRelativeNotePosition(Instance.GetTimeSignature(musicPosition), divisor, 1);

        float relativePosition = musicPosition - (int)musicPosition;

        int divisionIndex = (int)Math.Round(relativePosition / divisionLength);

        float snappedMusicPosition = (int)musicPosition + (divisionIndex * divisionLength);

        return snappedMusicPosition;
    }

    public void UpdateTimeSignature(int[] timeSignature, int musicPosition)
    {
        if (timeSignature[1] is not 4 and not 8 and not 16)
            timeSignature[1] = 4;
        if (timeSignature[0] == 0)
            timeSignature[0] = 4;
        else if (timeSignature[0] < 0)
            timeSignature[0] = -timeSignature[0];

        int foundTsPointIndex = TimeSignaturePoints.FindIndex(point => point.MusicPosition == musicPosition);

        TimeSignaturePoint timeSignaturePoint;

        if (foundTsPointIndex == -1)
        {
            timeSignaturePoint = new TimeSignaturePoint(timeSignature, musicPosition);
            TimeSignaturePoints.Add(timeSignaturePoint);
            TimeSignaturePoints.Sort();
            foundTsPointIndex = TimeSignaturePoints.FindIndex(point => point.MusicPosition == musicPosition);
        }
        else
        {
            timeSignaturePoint = TimeSignaturePoints[foundTsPointIndex];
            timeSignaturePoint.TimeSignature = timeSignature;
        }

        if (foundTsPointIndex > 0 && TimeSignaturePoints[foundTsPointIndex - 1].TimeSignature == timeSignature)
        {
            _ = TimeSignaturePoints.Remove(timeSignaturePoint);
            return;
        }

        if (foundTsPointIndex < TimeSignaturePoints.Count - 1 && TimeSignaturePoints[foundTsPointIndex + 1].TimeSignature == timeSignature)
            TimeSignaturePoints.RemoveAt(foundTsPointIndex + 1);

        // Go through all timing points until the next TimeSignaturePoint and update TimeSignature
        int maxIndex = TimingPoints.Count - 1;

        if (foundTsPointIndex < TimeSignaturePoints.Count - 1)
        {
            int nextMusicPositionWithDifferentTimeSignature = TimeSignaturePoints[foundTsPointIndex + 1].MusicPosition;
            maxIndex = TimingPoints.FindLastIndex(point => point.MusicPosition < nextMusicPositionWithDifferentTimeSignature);
        }

        int indexForFirstTimingPointWithThisTimeSignature = TimingPoints.FindIndex(point => point.MusicPosition >= musicPosition);

        for (int i = indexForFirstTimingPointWithThisTimeSignature; i <= maxIndex; i++)
        {
            if (i == -1)
                break;
            TimingPoint timingPoint = TimingPoints[i];
            TimingPoints[i].TimeSignature = timeSignature;
        }

        _ = Signals.Instance.EmitSignal("TimingChanged");
    }

    #endregion

    #region Calculators

    /// <summary>
    ///     Returns the <see cref="TimingPoint" /> at or right before a given music position. If none exist, returns the first
    ///     one after the music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public TimingPoint GetOperatingTimingPoint(float musicPosition)
    {
        if (TimingPoints.Count == 0)
            return null!;

        TimingPoint? timingPoint = TimingPoints.FindLast(point => point.MusicPosition <= musicPosition);

        // If there's only TimingPoints AFTER MusicPositionStart
        timingPoint ??= TimingPoints.Find(point => point.MusicPosition > musicPosition);

        return timingPoint == null
            ? throw new NullReferenceException("Timing point does not exist")
            : timingPoint.MusicPosition == null
            ? throw new NullReferenceException($"Operating TimingPoint does not have a non-null {nameof(TimingPoint.MusicPosition)}")
            : timingPoint;
    }

    public TimingPoint? GetPreviousTimingPoint(TimingPoint timingPoint)
    {
        int i = timingPoints.IndexOf(timingPoint);

        if (i == -1)
            GD.Print("Timing point is not present in the list of timing points");
        return i - 1 < 0 ? null : timingPoints[i - 1];
    }

    public TimingPoint? GetNextTimingPoint(TimingPoint timingPoint)
    {
        int i = timingPoints.IndexOf(timingPoint);

        if (i == -1)
            GD.Print("Timing point is not present in the list of timing points");
        return i + 1 >= timingPoints.Count ? null : timingPoints[i + 1];
    }

    public float? GetTimeDifference(int timingPointIndex1, int timingPointIndex2)
    {
        return timingPointIndex1 < 0 || timingPointIndex2 < 0 || timingPointIndex1 > TimingPoints.Count || timingPointIndex2 > TimingPoints.Count
            ? null
            : TimingPoints[timingPointIndex2].Time - TimingPoints[timingPointIndex1].Time;
    }

    public float MusicPositionToTime(float musicPosition)
    {
        TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
        if (timingPoint == null)
            return musicPosition / 0.5f; // default 120 bpm from time=0
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"Operating TimingPoint does not have a non-null {nameof(TimingPoint.MusicPosition)}");

        float time = (float)(timingPoint.Time + ((musicPosition - timingPoint.MusicPosition) / timingPoint.MeasuresPerSecond));

        return time;
    }

    public float TimeToMusicPosition(float time)
    {
        if (TimingPoints.Count == 0)
            return time * 0.5f; // default 120 bpm from time=0

        int timingPointIndex = TimingPoints.FindLastIndex(point => point.Time <= time);
        TimingPoint? timingPoint = timingPointIndex == -1 ? TimingPoints.Find(point => point.Time > time) : TimingPoints[timingPointIndex];
        if (timingPoint?.MusicPosition == null && timingPointIndex > 0)
            timingPoint = TimingPoints[timingPointIndex - 1];

        return timingPoint == null
            ? throw new NullReferenceException($"Operating {nameof(TimingPoint)} is null")
            : timingPoint?.MusicPosition == null
            ? throw new NullReferenceException($"Operating {nameof(TimingPoint)} has null {nameof(TimingPoint.MusicPosition)}")
            : (float)(((time - timingPoint.Time) * timingPoint.MeasuresPerSecond) + timingPoint.MusicPosition);

        // creating new Timing points with <= picks itself, tho there's no music position
    }

    public int[] GetTimeSignature(float musicPosition)
    {
        //TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
        //if (timingPoint == null) return new int[] { 4, 4 };
        //else return timingPoint.TimeSignature;

        TimeSignaturePoint? timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.MusicPosition <= musicPosition);
        return timeSignaturePoint == null ? (new[] { 4, 4 }) : timeSignaturePoint.TimeSignature;
    }

    /// <summary>
    /// Get the music-position length of a quarter-note
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public float GetBeatLength(float musicPosition)
    {
        int[] timeSignature = GetTimeSignature(musicPosition);
        return timeSignature[1] / 4f / timeSignature[0];
    }

    /// <summary>
    ///     Returns the music position of the beat at or right before the given music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public float GetBeatPosition(float musicPosition)
    {
        float beatLength = GetBeatLength(musicPosition);
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        float relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int beatsFromDownbeat = (int)(relativePosition / beatLength);
        float position = (beatsFromDownbeat * beatLength) + downbeatPosition;
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
        //GD.Print(Project.Instance);
        //GD.Print(Project.Instance.AudioFile);
        float lengthInSeconds = Project.Instance.AudioFile.SampleIndexToSeconds(Project.Instance.AudioFile.AudioData.Length - 1);
        float lastMeasure = TimeToMusicPosition(lengthInSeconds);
        return (int)lastMeasure;
    }

    /// <summary>
    /// Returns a copy of a timing object with additional timing poitns on downbeats and quarter-notes where necessary to make the timing rankable in osu.
    /// </summary>
    /// <param name="timing"></param>
    /// <returns></returns>
    public static Timing CopyAndAddExtraPoints(Timing timing)
    {

        //var newTiming = new Timing
        //{
        //    TimingPoints = timing.TimingPoints.ToList(),
        //    TimeSignaturePoints = timing.TimeSignaturePoints.ToList()
        //};

        //var newTiming = new Timing
        //{
        //    TimingPoints = new List<TimingPoint>(timing.timingPoints),
        //    TimeSignaturePoints = new List<TimeSignaturePoint>(timing.TimeSignaturePoints),
        //};

        var newTiming = new Timing
        {
            TimingPoints = CloneList<TimingPoint>(timing.timingPoints),
            TimeSignaturePoints = CloneList<TimeSignaturePoint>(timing.TimeSignaturePoints)
        };

        // Add extra downbeat timing points
        var downbeatPositions = new List<int>();
        foreach (TimingPoint timingPoint in timing.TimingPoints)
        {
            if (timingPoint?.MusicPosition == null)
                break;
            //if (timingPoint.NextTimingPoint == null) break;
            if (timingPoint.MusicPosition % 1 == 0)
                continue; // downbeat point on next is unnecessary
            //if (timingPoint.NextTimingPoint != null && (int)(timingPoint?.NextTimingPoint.MusicPosition) == (int)timingPoint.MusicPosition) continue; // next timing point is in same measure
            //if (timingPoint.NextTimingPoint != null && timingPoint.NextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition + 1) continue; // downbeat point on next measure already exists
            TimingPoint nextTimingPoint = Timing.Instance.GetNextTimingPoint(timingPoint);
            if (nextTimingPoint?.MusicPosition != null && (int)nextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition)
                continue; // next timing point is in same measure
            if (nextTimingPoint?.MusicPosition != null && nextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition + 1)
                continue; // downbeat point on next measure already exists
            downbeatPositions.Add((int)timingPoint.MusicPosition + 1);
        }
        foreach (int downbeat in downbeatPositions)
        {
            float time = newTiming.MusicPositionToTime(downbeat);
            newTiming.AddTimingPoint(downbeat, time);
        }

        // Add extra quarter-note timing points
        var quaterNotePositions = new List<float>();
        foreach (TimingPoint timingPoint in newTiming.TimingPoints)
        {
            if (timingPoint == null)
                break;
            //if (timingPoint.NextTimingPoint == null) break;
            if (timingPoint.MusicPosition == null)
                break;

            float beatLengthMP = timing.GetBeatLength((float)timingPoint.MusicPosition);
            float beatPosition = timing.GetBeatPosition((float)timingPoint.MusicPosition);
            //float? nextPointPosition = timingPoint?.NextTimingPoint?.MusicPosition;
            TimingPoint nextTimingPoint = Timing.Instance.GetNextTimingPoint(timingPoint);
            float? nextPointPosition = nextTimingPoint?.MusicPosition;

            if (timingPoint.MusicPosition % beatLengthMP == 0)
                continue; // is on quarter-note 
            //if (timingPoint.NextTimingPoint != null 
            //    && nextPointPosition <= beatPosition + beatLengthMP) 
            //    continue; // next timing point is on or before next quarter-note
            if (nextTimingPoint != null
                && nextPointPosition <= beatPosition + beatLengthMP)
            {
                continue; // next timing point is on or before next quarter-note
            }

            quaterNotePositions.Add(beatPosition + beatLengthMP);
        }
        foreach (float quarterNote in quaterNotePositions)
        {
            float time = newTiming.MusicPositionToTime(quarterNote);
            newTiming.AddTimingPoint(quarterNote, time);
        }

        return newTiming;
    }

    private static List<T> CloneList<T>(List<T> originalList) where T : ICloneable
    {
        List<T> clonedList = [];

        foreach (T item in originalList)
        {
            var clonedItem = (T)item.Clone();
            clonedList.Add(clonedItem);
        }

        return clonedList;
    }

    #endregion
}