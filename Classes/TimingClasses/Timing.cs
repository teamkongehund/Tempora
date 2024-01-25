using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Tempora.Classes.DataTools;
using GD = Tempora.Classes.DataTools.GD;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

/// <summary>
///     Data class controlling how the tempo of the song varies with time.
/// </summary>
public partial class Timing : Node, IMementoOriginator
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Instance = this;

    #region Properties & Signals

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
    private bool IsBatchOperatingInProgress = false;

    public static Timing Instance { get => instance; set => instance = value; }
    private static Timing instance = null!;
    
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



    #region Modify TimingPoint values

    /// <summary>
    ///     Snap a <see cref="TimingPoint" /> to the grid using <see cref="Settings.GridDivisor" /> and
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

        if (foundTsPointIndex == -1) // None found at same position
        {
            timeSignaturePoint = new TimeSignaturePoint(timeSignature, musicPosition);
            TimeSignaturePoints.Add(timeSignaturePoint);
            TimeSignaturePoints.Sort();
            foundTsPointIndex = TimeSignaturePoints.FindIndex(point => point.MusicPosition == musicPosition);
        }
        else // Update found point at position
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

        TimeSignaturesChanged?.Invoke(this, new Signals.ObjectArgument<TimeSignaturePoint>(timeSignaturePoint));
        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }

    /// <summary>
    /// Takes all TimingPoints after the measure of the time signature change 
    /// and alter their music positions such that the number of beats to them is kept the same.
    /// </summary>
    /// <param name="timeSignaturePoint"></param>
    /// <param name="oldTiming">Timing instance before the change occured</param>
    /// <exception cref="NullReferenceException"></exception>
    private void ShiftTimingPointsUponTimeSignatureChange(Timing oldTiming, TimeSignaturePoint timeSignaturePoint)
    {
        ArgumentNullException.ThrowIfNull(timeSignaturePoint);
        TimingPoint? operatingTimingPoint = GetOperatingTimingPoint_ByMusicPosition(timeSignaturePoint.MusicPosition);
        if (operatingTimingPoint == null)
            throw new NullReferenceException(nameof(operatingTimingPoint));

        int opIndex = TimingPoints.IndexOf(operatingTimingPoint);
        if (opIndex == -1)
            return;

        IsBatchOperatingInProgress = true;

        var newMusicPositions = new List<float>();

        for (int i = opIndex; opIndex < TimingPoints.Count; i++)
        {
            TimingPoint? timingPoint = TimingPoints[i];
            if (timingPoint == null)
                throw new NullReferenceException(nameof(timingPoint));

            if (TimingPoints[i].MusicPosition <= timeSignaturePoint.MusicPosition)
                return;

            if (timingPoint.MusicPosition == null)
                continue; 

            // Get current number of beats to timing Point with previous timing
            float beatsFromTheMeasureChangingToTimingPoint 
                = GetBeatsBetweenMusicPositions(timeSignaturePoint.MusicPosition, (float)timingPoint.MusicPosition);

            // TODO: Write from here
        }

        UpdateAllTimingPointsMPS();
        IsBatchOperatingInProgress = false;
    }

    /// <summary>
    /// Used after a batch operation since the normal method to update them was blocked by <see cref="IsBatchOperatingInProgress"/>
    /// </summary>
    private void UpdateAllTimingPointsMPS()
    {
        foreach (TimingPoint timingPoint in TimingPoints)
        {
            timingPoint.MeasuresPerSecond_Set(this);
        }
    }
    
    #endregion



    #region Calculators
    
    public bool CanTimingPointGoHere(TimingPoint? timingPoint, float musicPosition, out TimingPoint? rejectingTimingPoint)
    {
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        // validity checks
        if (previousTimingPoint != null && previousTimingPoint.MusicPosition >= musicPosition)
        {
            rejectingTimingPoint = previousTimingPoint;
            return false;
        }
        if (nextTimingPoint != null && nextTimingPoint.MusicPosition <= musicPosition)
        {
            rejectingTimingPoint = nextTimingPoint;
            return false;
        }

        rejectingTimingPoint = null;
        return true;
    }

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

}