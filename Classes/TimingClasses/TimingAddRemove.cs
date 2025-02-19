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
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

public partial class Timing
{
    /// <summary>
    /// Add a timing point. Primary constructor for loading timing points with a file.
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <param name="time"></param>
    public void AddTimingPoint(float measurePosition, float time)
    {
        var timingPoint = new TimingPoint(time, measurePosition, GetTimeSignature(measurePosition));
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();
        float? mps = CalculateMPSBasedOnAdjacentPoints(timingPoint);
        timingPoint.MeasuresPerSecond = mps == null ? timingPoint.MeasuresPerSecond : (float)mps;

        timingPoint.IsInstantiating = false;

        int index = TimingPoints.IndexOf(timingPoint);

        bool isIncompatibleWithPrevious = index > 0 && TimingPoints[index - 1].Offset > timingPoint.Offset;
        bool isIncompatibleWithNext = index < TimingPoints.Count - 1 && TimingPoints[index + 1].Offset < timingPoint.Offset;
        if (isIncompatibleWithPrevious || isIncompatibleWithNext)
            throw new System.Exception("Timing point was incompatible with neighboring timing points.");

        if (index >= 1) // Update previous timing point
        {
            UpdateMPS(TimingPoints[index - 1]);
        }
        if (index < TimingPoints.Count - 1) // Update next timing point
        {
            UpdateMPS(TimingPoints[index + 1]);
        }
        if (!IsInstantiating)
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
    }

    /// <summary>
    /// Add a timing point and deduce time and MPS from other Timing Points. Useful for adding extra points on downbeats and quarter notes.
    /// This method should not be accessible from the GUI.
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <param name="time"></param>
    public void AddTimingPoint(float measurePosition)
    {
        var timingPoint = new TimingPoint(measurePosition);
        timingPoint.Offset = MeasurePositionToOffset(measurePosition);
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        // This should also update MPS
        timingPoint.TimeSignature = GetTimeSignature(measurePosition);

        timingPoint.IsInstantiating = false;

        // No timing memento added, as this isn't method shouldn't be executed directly from a user action.
        //ActionsHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    /// Add a timing point and force a given <see cref="TimingPoint.MeasuresPerSecond"/> value without checking validity.
    /// </summary>
    public void AddTimingPoint(float measurePosition, float time, float measuresPerSecond)
    {
        var timingPoint = new TimingPoint(time, measurePosition, GetTimeSignature(measurePosition), measuresPerSecond);
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        timingPoint.IsInstantiating = false;

        if (!IsInstantiating)
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));

        int index = TimingPoints.IndexOf(timingPoint);

        if (index >= 1) // Update previous timing point
        {
            UpdateMPS(TimingPoints[index - 1]);

            if (!IsInstantiating)
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
        }
    }

    /// <summary>
    ///     Add a <see cref="TimingPoint"/> at a given time. Primary GUI method.
    ///     The <see cref="TimingPoint.MeasurePosition"/> is defined via the existing timing.
    /// </summary>
    /// <param name="time"></param>
    public void AddTimingPoint(float time, out TimingPoint? timingPoint)
    {
        timingPoint = new TimingPoint(time, GetTimeSignature(OffsetToMeasurePosition(time)));
        TimingPoints.Add(timingPoint);
        TimingPoints.Sort();

        SubscribeToEvents(timingPoint);

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        timingPoint.MeasurePosition = OffsetToMeasurePosition(time);

        if (timingPoint.MeasurePosition == null
            || previousTimingPoint?.MeasurePosition == timingPoint.MeasurePosition
            || nextTimingPoint?.MeasurePosition == timingPoint.MeasurePosition
            || (previousTimingPoint?.MeasurePosition is float previousMeasurePosition && Mathf.Abs(previousMeasurePosition - (float)timingPoint.MeasurePosition) < 0.015f) // Too close to previous timing point
            || (nextTimingPoint?.MeasurePosition is float nextMeasurePosition && Mathf.Abs(nextMeasurePosition - (float)timingPoint.MeasurePosition) < 0.015f) // Too close to next timing point
           )
        {
            TimingPoints.Remove(timingPoint);
            timingPoint = null;
            //GD.Print("Timing Point refused to add!");
            return;
        }

        timingPoint.IsInstantiating = false;

        //ActionsHandler.Instance.AddTimingMemento();

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
    }

    private void DeleteTimingPoint(TimingPoint timingPoint)
    {
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);

        timingPoint.QueueFree();
        TimingPoints.Remove(timingPoint);

        if (previousTimingPoint != null && TimingPoints.IndexOf(previousTimingPoint) != TimingPoints.Count - 1)
            UpdateMPS(previousTimingPoint);

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));

        MementoHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    /// Delete selection if the <see cref="TimingPoint"/> is in the selection. Otherwise, delete the timing point.
    /// </summary>
    /// <param name="timingPoint"></param>
    public void DeleteTimingPointOrSelection(TimingPoint? timingPoint)
    {
        if (timingPoint == null)
            return;
        if (TimingPointSelection.Instance.IsPointInSelection(timingPoint))
            TimingPointSelection.Instance.DeleteSelection();
        else
            DeleteTimingPoint(timingPoint);
    }

    /// <summary>
    /// Deletes timing points between index excluding indexTo.
    /// </summary>
    /// <param name="indexFrom"></param>
    /// <param name="indexTo"></param>
    public void DeleteTimingPoints(int indexFrom, int indexTo)
    {
        if (TimingPoints.Count == 0) return;
        TimingPoint? previousTimingPoint = (indexFrom - 1) >= 0 ? TimingPoints[(indexFrom - 1)] : null;
        bool isIndexToLast = indexTo == TimingPoints.Count;

        for (int i = indexFrom; i < indexTo; i++)
        {
            TimingPoint timingPoint = TimingPoints[i];
            timingPoint.QueueFree();
        }
        TimingPoints.RemoveRange(indexFrom, indexTo - indexFrom);

        if (!isIndexToLast && previousTimingPoint != null)
            UpdateMPS(previousTimingPoint);

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));

        MementoHandler.Instance.AddTimingMemento();
    }


    public void DeleteAllTimingPoints()
    {
        DeleteTimingPoints(0, TimingPoints.Count);
    }
    
    /// <summary>
    /// Delete all time signature points without changing any timing points.
    /// </summary>
    public void DeleteAllTimeSignaturePoints()
    {
        TimeSignaturePoints.Clear();
        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
    }   

    private void RemovePointsThatChangeNothing()
    {
        var pointsToDelete = new List<TimingPoint>();

        foreach (TimingPoint timingPoint in TimingPoints)
        {
            var previous = GetPreviousTimingPoint(timingPoint);
            if (previous == null)
                continue;
            bool mpsIsSame = MathF.Abs(previous.MeasuresPerSecond - (float)timingPoint.MeasuresPerSecond) < 0.0001;
            bool ts0IsSame = previous?.TimeSignature[0] == timingPoint.TimeSignature[0];
            bool ts1IsSame = previous?.TimeSignature[1] == timingPoint.TimeSignature[1];

            if (mpsIsSame && ts0IsSame && ts1IsSame)
                pointsToDelete.Add(timingPoint);
        }

        foreach (TimingPoint timingPoint in pointsToDelete)
        {
            DeleteTimingPoint(timingPoint);
        }
    }

    private void AddExtraPointsOnDownbeats()
    {
        var downbeatPositions = new List<int>();
        foreach (TimingPoint timingPoint in TimingPoints)
        {
            if (timingPoint?.MeasurePosition == null)
                break;
            if (timingPoint.MeasurePosition % 1 == 0)
                continue; // downbeat point on next is unnecessary
            TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);
            bool isNextPointInSameMeasure = nextTimingPoint?.MeasurePosition != null
                && (int)nextTimingPoint.MeasurePosition == (int)timingPoint.MeasurePosition;
            bool isThereAPointOnNextDownbeat = nextTimingPoint?.MeasurePosition != null
                && nextTimingPoint.MeasurePosition == (int)timingPoint.MeasurePosition + 1;
            if (isNextPointInSameMeasure || isThereAPointOnNextDownbeat)
                continue;
            downbeatPositions.Add((int)timingPoint.MeasurePosition + 1);
        }
        foreach (int downbeat in downbeatPositions)
        {
            //float time = newTiming.MeasurePositionToTime(downbeat);
            //newTiming.AddTimingPoint(downbeat, time);
            AddTimingPoint(downbeat);
        }
    }
    private void AddExtraPointsOnQuarterNotes()
    {
        var quaterNotePositions = new List<float>();
        foreach (TimingPoint timingPoint in TimingPoints)
        {
            if (timingPoint == null)
                break;
            if (timingPoint.MeasurePosition == null)
                break;

            float beatLength = GetDistancePerBeat((float)timingPoint.MeasurePosition);
            float beatPosition = GetOperatingBeatPosition((float)timingPoint.MeasurePosition);
            TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);
            float? nextPointPosition = nextTimingPoint?.MeasurePosition;

            //float epsilon = 0.00001f;
            //bool isOnQuarterNote = (timingPoint.MeasurePosition % beatLength < epsilon || (beatLength - timingPoint.MeasurePosition % beatLength) < epsilon);

            bool isOnQuarterNote = IsPositionOnDivisor((float)timingPoint.MeasurePosition, timingPoint.TimeSignature, 4);

            bool nextPointIsOnOrBeforeNextQuarterNote = (nextTimingPoint != null
                && nextPointPosition <= beatPosition + beatLength);
            if (isOnQuarterNote || nextPointIsOnOrBeforeNextQuarterNote)
                continue;

            quaterNotePositions.Add(beatPosition + beatLength);
        }
        foreach (float quarterNote in quaterNotePositions)
        {
            //float time = newTiming.MeasurePositionToTime(quarterNote);
            //newTiming.AddTimingPoint(quarterNote, time);
            AddTimingPoint(quarterNote);
        }
    }

    private void AddExtraPointsOnUnsupportedSignatures(AudioFile audioFile)
    {
        // Maybe add exceptions later like 4/8 and 8/8 

        int firstMeasure = (int)OffsetToMeasurePosition(0f);
        int lastMeasure = (int)OffsetToMeasurePosition((float)audioFile.Stream.GetLength());

        for (int measure = firstMeasure; measure < lastMeasure + 1; measure++)
        {
            int[] timeSignature = GetTimeSignature(measure);
            if (timeSignature[1] == 4) continue;

            TimingPoint? operatingPoint = GetOperatingTimingPoint_ByMeasurePosition(measure);

            if (operatingPoint?.MeasurePosition == measure)
                continue;

            AddTimingPoint(measure);
        }
    }
}