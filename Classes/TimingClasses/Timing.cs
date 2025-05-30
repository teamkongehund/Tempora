// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Tempora.Classes.DataHelpers;
using GD = Tempora.Classes.DataHelpers.GD;
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
    public bool IsBatchOperationInProgress = false;

    private bool ShouldCancelBatchOperation = false;

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

    /// <summary>
    /// Checks if a new <see cref="TimingPoint.MeasurePosition"/> is valid, by checking whether it crosses the position of other points.
    /// </summary>
    /// <param name="timingPoint"></param>
    /// <param name="measurePosition"></param>
    /// <param name="rejectingTimingPoint"></param>
    /// <returns></returns>
    public bool CanTimingPointGoHere(TimingPoint? timingPoint, float? measurePosition, out TimingPoint? rejectingTimingPoint)
    {
        if (measurePosition == null)
        {
            rejectingTimingPoint = null;
            return true;
        }

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        // validity checks
        if (previousTimingPoint != null 
            && (previousTimingPoint.MeasurePosition >= measurePosition || AreMeasurePositionsEqual(previousTimingPoint.MeasurePosition, measurePosition)))
        {
            rejectingTimingPoint = previousTimingPoint;
            return false;
        }
        if (nextTimingPoint != null 
            && (nextTimingPoint.MeasurePosition <= measurePosition || AreMeasurePositionsEqual(nextTimingPoint.MeasurePosition, measurePosition)))
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

    /// <summary>
    /// Converts a music position to number of seconds from first sample. 
    /// Note that mp3 playback does not start at first sample, so must be transformed if used in a playback context.
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="Exception"></exception>
    public float MeasurePositionToOffset(float measurePosition)
    {
        TimingPoint? timingPoint = GetOperatingTimingPoint_ByMeasurePosition(measurePosition);
        if (timingPoint == null)
            return measurePosition / 0.5f; // default 120 bpm from time=0
        if (timingPoint.MeasurePosition == null)
            throw new NullReferenceException($"Operating TimingPoint does not have a non-null {nameof(TimingPoint.MeasurePosition)}");

        if (timingPoint.MeasuresPerSecond <= 0)
            throw new Exception("Operating timing point has MeasuresPerSecond <= 0");

        float time = (float)(timingPoint.Offset + ((measurePosition - timingPoint.MeasurePosition) / timingPoint.MeasuresPerSecond));

        return time;
    }
    
    /// <summary>
    /// Checks whether a music position division (defined by a divisor and an index) is divisible by a different divisor.
    /// Example: (divisor = 12, index = 7, otherIndex = 4) should return true because the 7th 12th note has the same relative music position as the 2nd 4th note.
    /// </summary>
    /// <param name="otherDivisor"></param>
    /// <param name="index"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static bool IsDivisionOnDivisor(int divisor, int index, int otherDivisor)
    {
        int[] timeSignature = [4,4]; // The time signature doesn't matter in this case.
        float relativePosition = GetRelativeNotePosition(timeSignature, divisor, index);

        return IsPositionOnDivisor(relativePosition, timeSignature, otherDivisor);
    }

    /// <summary>
    /// Checks whether a music position is divisible by a different divisor for a given time signature.
    /// Example: For a 4/4 time signature, music position 0.5 and divisor 4 should return true, as that position is on the 3rd quarter note.
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <param name="timeSignature"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static bool IsPositionOnDivisor(float measurePosition, int[] timeSignature, int divisor)
    {
        float divisorLength = GetRelativeNotePosition(timeSignature, divisor, 1);

        measurePosition = measurePosition % 1;

        var epsilon = 0.001f;
        bool isDivisible = (measurePosition % divisorLength < epsilon || (divisorLength - measurePosition % divisorLength) < epsilon);

        return isDivisible;
    }

    public static bool AreMeasurePositionsEqual(TimingPoint? timingPoint1, TimingPoint? timingPoint2) 
        => AreMeasurePositionsEqual(timingPoint1?.MeasurePosition, timingPoint2?.MeasurePosition);

    public static bool AreMeasurePositionsEqual(float? measurePosition1, float? measurePosition2)
    {
        if (measurePosition1 == null || measurePosition2 == null) 
            return false;
        return MathF.Abs((float)measurePosition1 - (float)measurePosition2) < 0.001;
    }
    
    public bool CanBpmBeChangedManually(TimingPoint timingPoint)
    {
        var timingPoints = Timing.Instance.TimingPoints;
        if (timingPoints.Count == 0)
            throw new ArgumentException($"{nameof(TimingPoint)} was not a part of {nameof(Timing)}");

        return timingPoints[timingPoints.Count - 1] == timingPoint;
    }
    #endregion

}