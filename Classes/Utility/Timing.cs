using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace OsuTimer.Classes.Utility;

/// <summary>
///     Data class controlling how the tempo of the song varies with time.
/// </summary>
public partial class Timing : Node , IMementoOriginator
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Instance = this;

    #region Properties & Signals
    #region Object-related Properties
    private bool isInstantiating;

    public bool IsInstantiating
    {
        get => isInstantiating;
        set
        {
            if (isInstantiating == value)
                return;
            isInstantiating = value;
        }
    }


    public static Timing Instance { get => instance; set => instance = value; }

    //public AudioFile AudioFile;

    private static Timing instance = null!; 
    #endregion
    #region Timing-related Properties
    private List<TimingPoint> timingPoints = [];

    public List<TimingPoint> TimingPoints
    {
        get => timingPoints;
        set
        {
            if (timingPoints == value)
                return;
            timingPoints = value;
            ReSubscribe();
            Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
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
            Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
        }
    } 
    #endregion
    #endregion
    #region Timing modification

    #region Event connections and responses
    private void SubscribeToEvents(TimingPoint timingPoint)
    {
        if (timingPoint == null)
            throw new NullReferenceException($"{nameof(timingPoint)} was null");
        timingPoint.AttemptDelete += OnTimingPointDeleteAttempt;
        timingPoint.MPSUpdateRequested += OnTimingPointRequestingToUpdateMPS;
        timingPoint.Changed += OnTimingPointChanged;
    }

    private void ReSubscribe()
    {
        foreach (TimingPoint timingPoint in timingPoints)
        {
            SubscribeToEvents(timingPoint);
        }
    }

    private void OnTimingPointChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");

        if (!timingPoint.IsInstantiating)
            Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }
    private void OnTimingPointDeleteAttempt(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        DeleteTimingPoint(timingPoint);
    }
    private void OnTimingPointRequestingToUpdateMPS(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"Request to update {nameof(timingPoint.MeasuresPerSecond)} failed because {nameof(timingPoint.MusicPosition)} is null");

        timingPoint.MeasuresPerSecond_Set(this);
        if (!timingPoint.IsInstantiating) 
            GetPreviousTimingPoint(timingPoint)?.MeasuresPerSecond_Set(this);
    }

    #endregion

    #region Add/Delete TimingPoint

    /// <summary>
    /// Add a timing point. Primary constructor for loading timing points with a file.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <param name="time"></param>
    public void AddTimingPoint(float musicPosition, float time)
    {
        var timingPoint = new TimingPoint(time, musicPosition, GetTimeSignature(musicPosition));
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        timingPoint.IsInstantiating = false;

        int index = TimingPoints.IndexOf(timingPoint);

        if (index >= 1) // Set previous timing point
        {
            TimingPoints[index - 1].MeasuresPerSecond_Set(this);

            if (!IsInstantiating)
                Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
        }
    }

    /// <summary>
    /// Add a timing point and force a given <see cref="TimingPoint.MeasuresPerSecond"/> value without checking validity.
    /// </summary>
    public void AddTimingPoint(float musicPosition, float time, float measuresPerSecond)
    {
        var timingPoint = new TimingPoint(time, musicPosition, GetTimeSignature(musicPosition), measuresPerSecond);
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        timingPoint.IsInstantiating = false;

        if (!IsInstantiating)
            Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }

    /// <summary>
    ///     Add a <see cref="TimingPoint"/> at a given time. 
    ///     The <see cref="TimingPoint.MusicPosition"/> is defined via the existing timing.
    /// </summary>
    /// <param name="time"></param>
    public void AddTimingPoint(float time, out TimingPoint? timingPoint)
    {
        timingPoint = new TimingPoint(time, GetTimeSignature(TimeToMusicPosition(time)));
        TimingPoints.Add(timingPoint);
        TimingPoints.Sort();

        SubscribeToEvents(timingPoint);

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        timingPoint.MusicPosition_Set(TimeToMusicPosition(time), this);

        if (timingPoint.MusicPosition == null
            || previousTimingPoint?.MusicPosition == timingPoint.MusicPosition
            || nextTimingPoint?.MusicPosition == timingPoint.MusicPosition 
            || (previousTimingPoint?.MusicPosition is float previousMusicPosition && Mathf.Abs(previousMusicPosition - (float)timingPoint.MusicPosition) < 0.015f) // Too close to previous timing point
            || (nextTimingPoint?.MusicPosition is float nextMusicPosition && Mathf.Abs(nextMusicPosition - (float)timingPoint.MusicPosition) < 0.015f) // Too close to next timing point
           )
        {
            TimingPoints.Remove(timingPoint);
            timingPoint = null;
            //GD.Print("Timing Point refused to add!");
            return;
        }

        timingPoint.IsInstantiating = false;

        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }

    private void DeleteTimingPoint(TimingPoint timingPoint)
    {
        TimingPoints.IndexOf(timingPoint);

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);

        timingPoint.QueueFree();
        TimingPoints.Remove(timingPoint);

        previousTimingPoint?.MeasuresPerSecond_Set(this);

        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);

        ActionsHandler.Instance.AddTimingMemento();
    }

    #endregion

    #region Modify TimingPoint values

    /// <summary>
    ///     Snap a <see cref="TimingPoint" /> to the grid using <see cref="Settings.Divisor" /> and
    ///     <see cref="Settings.SnapToGridEnabled" />
    /// </summary>
    /// <param name="timingPoint"></param>
    /// <param name="musicPosition"></param>
    public void SnapTimingPoint(TimingPoint timingPoint, float musicPosition)
    {
        if (timingPoint == null)
            return;

        float snappedMusicPosition = SnapMusicPosition(musicPosition);
        timingPoint.MusicPosition_Set(snappedMusicPosition, this);
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
            TimeSignaturePoints.Remove(timeSignaturePoint);
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

        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }

    //private void UpdateMeasuresPerSecond(TimingPoint timingPoint) => timingPoint.MeasuresPerSecond_Set(this);
    #endregion

    #endregion
    #region Calculators

    #region TimingPoint
    /// <summary>
    ///     Returns the <see cref="TimingPoint" /> at or right before a given music position. If none exist, returns the first
    ///     one after the music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public TimingPoint? GetOperatingTimingPoint_ByMusicPosition(float musicPosition)
    {
        if (TimingPoints.Count == 0)
            return null;

        TimingPoint? timingPoint = TimingPoints.FindLast(point => point.MusicPosition <= musicPosition);

        // If there's only TimingPoints AFTER MusicPositionStart
        timingPoint ??= TimingPoints.Find(point => point.MusicPosition > musicPosition);

        return timingPoint == null
            ? throw new NullReferenceException("Timing point does not exist")
            : timingPoint.MusicPosition == null
            ? throw new NullReferenceException($"Operating TimingPoint does not have a non-null {nameof(TimingPoint.MusicPosition)}")
            : timingPoint;
    }

    public TimingPoint? GetOperatingTimingPoint_ByTime(float time)
    {
        // Ensures the method can be used while a TimingPoint is being created.
        var validTimingPoints = TimingPoints.Where(point => point.MusicPosition != null).ToList<TimingPoint>();

        if (validTimingPoints == null)
            return null;

        int operatingTimingPointIndex = validTimingPoints.FindLastIndex(point => point.Offset <= time);
        TimingPoint? operatingTimingPoint = operatingTimingPointIndex == -1 ? TimingPoints.Find(point => point.Offset > time) : validTimingPoints[operatingTimingPointIndex];

        return operatingTimingPoint;
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
    #endregion
    #region Time
    public float? GetTimeDifference(int timingPointIndex1, int timingPointIndex2)
    {
        return timingPointIndex1 < 0 || timingPointIndex2 < 0 || timingPointIndex1 > TimingPoints.Count || timingPointIndex2 > TimingPoints.Count
            ? null
            : TimingPoints[timingPointIndex2].Offset - TimingPoints[timingPointIndex1].Offset;
    }

    public float MusicPositionToTime(float musicPosition)
    {
        TimingPoint? timingPoint = GetOperatingTimingPoint_ByMusicPosition(musicPosition);
        if (timingPoint == null)
            return musicPosition / 0.5f; // default 120 bpm from time=0
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"Operating TimingPoint does not have a non-null {nameof(TimingPoint.MusicPosition)}");

        float time = (float)(timingPoint.Offset + ((musicPosition - timingPoint.MusicPosition) / timingPoint.MeasuresPerSecond));

        return time;
    }
    #endregion
    #region MusicPosition
    public float TimeToMusicPosition(float time)
    {
        TimingPoint? operatingTimingPoint = GetOperatingTimingPoint_ByTime(time);

        if (operatingTimingPoint?.MusicPosition == null)
        {
            return time * 0.5f; // default 120 bpm from musicposition origin
        }
        else 
            return (float)(((time - operatingTimingPoint.Offset) * operatingTimingPoint.MeasuresPerSecond) + operatingTimingPoint.MusicPosition);
    }

    public int[] GetTimeSignature(float musicPosition)
    {
        //TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
        //if (timingPoint == null) return new int[] { 4, 4 };
        //else return timingPoint.TimeSignature;

        TimeSignaturePoint? timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.MusicPosition <= musicPosition);
        return timeSignaturePoint == null ? ([4, 4]) : timeSignaturePoint.TimeSignature;
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

    #endregion

    #endregion
    #region Cloning
    /// <summary>
    /// Returns a copy of a timing object with additional timing poitns on downbeats and quarter-notes where necessary to make the timing rankable in osu.
    /// </summary>
    /// <param name="timing"></param>
    /// <returns></returns>
    public static Timing CopyAndAddExtraPoints(Timing timing)
    {
        var newTiming = new Timing
        {
            TimingPoints = CloneUtility.CloneList<TimingPoint>(timing.timingPoints),
            TimeSignaturePoints = CloneUtility.CloneList<TimeSignaturePoint>(timing.TimeSignaturePoints)
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
            TimingPoint? nextTimingPoint = Timing.Instance.GetNextTimingPoint(timingPoint);
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
            TimingPoint? nextTimingPoint = Timing.Instance.GetNextTimingPoint(timingPoint);
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

    #endregion

    #region Memento
    public IMemento GetMemento()
    {
        return new TimingMemento(this);
    }

    public void RestoreMemento(IMemento memento)
    {
        ArgumentNullException.ThrowIfNull(memento);

        if (memento is not TimingMemento timingMemento)
            throw new ArgumentException($"{nameof(memento)} was not of type {nameof(TimingMemento)}");

        TimingPoints = timingMemento.clonedTimingPoints;
        TimeSignaturePoints = timingMemento.clonedTimeSignaturePoints;
    }

    private class TimingMemento(Timing originator) : IMemento
    {
        public readonly List<TimingPoint> clonedTimingPoints = CloneUtility.CloneList(originator.timingPoints);
        public readonly List<TimeSignaturePoint> clonedTimeSignaturePoints = CloneUtility.CloneList(originator.timeSignaturePoints);

        public IMementoOriginator GetOriginator() => originator;
    } 
    #endregion
}