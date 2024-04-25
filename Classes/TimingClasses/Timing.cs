using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Tempora.Classes.DataTools;
using GD = Tempora.Classes.DataTools.GD;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

/// <summary>
///     Data class controlling how the tempo of the song varies with time. Class is split into multiple scripts as it's pretty big.
/// </summary>
public partial class Timing : Node, IMementoOriginator
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            Name = "The Timing Singleton";
        }
        else
        {
            throw new Exception("A new Timing instance tried to enter the Godot scene tree despite a Timing singleton already exisiting");
        }
    }


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
    private bool IsBatchOperationInProgress = false;

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
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
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
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
        }
    }
    
    #endregion






    #region Calculators
    
    // These methods can be moved to separate scripts if necessary.

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

        if (timingPoint.MeasuresPerSecond <= 0)
            throw new Exception("Operating timing point has MeasuresPerSecond <= 0");

        float time = (float)(timingPoint.Offset + ((musicPosition - timingPoint.MusicPosition) / timingPoint.MeasuresPerSecond));

        return time;
    }
    
    #endregion

}