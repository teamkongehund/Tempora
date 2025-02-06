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
using System.Linq;
using Tempora.Classes.Utility;
using GD = Tempora.Classes.DataHelpers.GD;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    /// <summary>
    ///     Snap a <see cref="TimingPoint" /> to the grid using <see cref="Settings.GridDivisor" /> and
    ///     <see cref="Settings.SnapToGridEnabled" />
    /// </summary>
    /// <param name="timingPoint"></param>
    /// <param name="measurePosition"></param>
    public void SnapTimingPoint(TimingPoint timingPoint, float measurePosition)
    {
        if (timingPoint == null)
            return;

        float snappedMeasurePosition = SnapMeasurePosition(measurePosition);

        timingPoint.MeasurePosition = snappedMeasurePosition;
    }

    public void UpdateTimeSignature(int[] timeSignature, int measurePosition)
    {
        CorrectTimeSignature(timeSignature, out timeSignature);

        if (GetTimeSignature(measurePosition).SequenceEqual(timeSignature))
            return;

        // epsilon necessary due to time sig moving points. To-do: Better long-term fix is to snap to best possible gridline upon time sig changes.
        int foundTsPointIndex = TimeSignaturePoints.FindIndex(point => MathF.Abs(point.Measure - measurePosition) < 0.005); 

        Timing oldTiming = CopyTiming(this);

        TimeSignaturePoint timeSignaturePoint;

        // Logic decisions:
        // We always have 4/4 at mp = -infinity
        // If the value submitted is 4/4 and it's the first time sig point, remove it from list

        if (foundTsPointIndex == -1) // None found at same position
        {
            timeSignaturePoint = new TimeSignaturePoint(timeSignature, measurePosition);
            TimeSignaturePoints.Add(timeSignaturePoint);
            TimeSignaturePoints.Sort();
            foundTsPointIndex = TimeSignaturePoints.FindIndex(point => point.Measure == measurePosition);
        }
        else // Update found point at position
        {
            timeSignaturePoint = TimeSignaturePoints[foundTsPointIndex];
            timeSignaturePoint.TimeSignature = timeSignature;
        }

        bool isEqualToPrevious = foundTsPointIndex > 0 && TimeSignaturePoints[foundTsPointIndex - 1].TimeSignature.SequenceEqual(timeSignature);
        bool isFirstAndDefault = foundTsPointIndex == 0 && timeSignaturePoint.TimeSignature.SequenceEqual([4, 4]);

        if (isEqualToPrevious || isFirstAndDefault)
        {
            TimeSignaturePoints.Remove(timeSignaturePoint);
        }

        bool isEqualToNext = foundTsPointIndex<TimeSignaturePoints.Count -1 && TimeSignaturePoints[foundTsPointIndex + 1].TimeSignature.SequenceEqual(timeSignature);
       
        if (isEqualToNext) // To-do: Fix this. Try having a 5/4 a 6/4 and 5/4, then change the 6/4 to 5/4. Doesn't work as expected. Maybe this one before the other?
            TimeSignaturePoints.Remove(TimeSignaturePoints[foundTsPointIndex + 1]);

        // Go through all timing points until the next TimeSignaturePoint and update TimeSignature
        int maxIndex = TimingPoints.Count - 1;

        if (foundTsPointIndex < TimeSignaturePoints.Count - 1)
        {
            int nextMeasurePositionWithDifferentTimeSignature = TimeSignaturePoints[foundTsPointIndex + 1].Measure;
            maxIndex = TimingPoints.FindLastIndex(point => point.MeasurePosition < nextMeasurePositionWithDifferentTimeSignature);
        }

        int indexForFirstTimingPointWithThisTimeSignature = TimingPoints.FindIndex(point => point.MeasurePosition >= measurePosition);

        IsBatchOperationInProgress = true;
        for (int i = indexForFirstTimingPointWithThisTimeSignature; i <= maxIndex; i++)
        {
            if (i == -1)
                break;
            TimingPoint timingPoint = TimingPoints[i];
            TimingPoints[i].TimeSignature = timeSignature;
        }
        IsBatchOperationInProgress = false;

        if (TimingPoints.Find(point => point.MeasurePosition == measurePosition) == null && TimingPoints.Count > 0)
            AddTimingPoint(measurePosition, MeasurePositionToSampleTime(measurePosition));

        ShiftTimingPointsUponTimeSignatureChange(oldTiming, timeSignaturePoint);

        MementoHandler.Instance.AddTimingMemento();

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
    }

    public static void CorrectTimeSignature(int[] timeSignature, out int[] correctedTimeSignature)
    {
        if (timeSignature[1] is not 4 and not 8 and not 16)
            timeSignature[1] = 4;
        if (timeSignature[0] == 0)
            timeSignature[0] = 1;
        else if (timeSignature[0] < 0)
            timeSignature[0] = -timeSignature[0];
        correctedTimeSignature = timeSignature;
    }

    /// <summary>
    /// Takes all TimingPoints after the measure of the time signature change 
    /// and alter their music positions such that the number of beats to them is kept the same.
    /// </summary>
    /// <param name="timeSignaturePoint"></param>
    /// <param name="oldTiming">Timing instance before the change occured</param>
    private void ShiftTimingPointsUponTimeSignatureChange(Timing oldTiming, TimeSignaturePoint timeSignaturePoint)
    {
        if (!Settings.Instance.MoveSubsequentTimingPointsWhenChangingTimeSignature)
            return;

        ArgumentNullException.ThrowIfNull(timeSignaturePoint);
        TimingPoint? operatingTimingPoint = GetOperatingTimingPoint_ByMeasurePosition(timeSignaturePoint.Measure);
        if (operatingTimingPoint == null)
            return;

        int opIndex = TimingPoints.IndexOf(operatingTimingPoint);
        if (opIndex == -1)
            return;

        float getNewMeasurePosition(TimingPoint? timingPoint)
        {
            if (timingPoint?.MeasurePosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            // Get number of beats from time signature change to timingPoint's old measurePosition using previous timing
            // This should be kept constant
            float beatDifference_OldTiming
                = GetBeatsBetweenMeasurePositions(oldTiming, timeSignaturePoint.Measure, (float)timingPoint.MeasurePosition);

            float beatDifference_NewTiming
                = GetBeatsBetweenMeasurePositions(this, timeSignaturePoint.Measure, (float)timingPoint.MeasurePosition);

            float beatsToAdd = beatDifference_OldTiming - beatDifference_NewTiming;

            // Get new music position for this timing point
            float newMeasurePosition = GetMeasurePositionAfterAddingBeats(this, (float)timingPoint.MeasurePosition, beatsToAdd);

            return newMeasurePosition;
        }

        BatchChangeMeasurePosition(opIndex, TimingPoints.Count - 1, getNewMeasurePosition);
    }

    /// <summary>
    /// Delegate that defines what a new music position should be for a timing point.
    /// </summary>
    public delegate float GetNewMeasurePosition(TimingPoint? timingPoint);

    /// <summary>
    /// Changes the music positions of all <see cref="TimingPoint"/>s from <see cref="TimingPoints"/>[lowerIndex] to and including <see cref="TimingPoints"/>[higherIndex].
    /// </summary>
    /// <param name="getNewMeasurePosition">Delegate method used to calculate the new music position</param>
    /// <exception cref="NullReferenceException"></exception>
    public void BatchChangeMeasurePosition(int lowerIndex, int higherIndex, GetNewMeasurePosition getNewMeasurePosition)
    {
        if (!ValidateIndices(lowerIndex, higherIndex, out lowerIndex, out higherIndex))
            return;

        IsBatchOperationInProgress = true;

        bool willMeasurePositionsIncrease = true;
        if (higherIndex >= lowerIndex + 1)
            willMeasurePositionsIncrease = getNewMeasurePosition(TimingPoints[lowerIndex + 1]) > TimingPoints[lowerIndex + 1].MeasurePosition;

        // If the change decreases the music position, iterate forwards (less likely to trigger rejections).
        // Vice versa for increases
        int startIndex = willMeasurePositionsIncrease ? higherIndex : lowerIndex;
        for (int i = startIndex
            ; willMeasurePositionsIncrease ? i >= lowerIndex : i <= higherIndex
            ; i = willMeasurePositionsIncrease ? i - 1 : i + 1)
        {
            TimingPoint? timingPoint = TimingPoints[i];
            if (timingPoint?.MeasurePosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            timingPoint.MeasurePosition = getNewMeasurePosition(timingPoint);

            if (ShouldCancelBatchOperation)
            {
                ShouldCancelBatchOperation = false;
                break;
            }
        }

        IsBatchOperationInProgress = false;

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
    }

    /// <summary>
    /// Scales Tempo of selected timing points (including higherIndex) by a multiplier. Adds a <see cref="TimingMemento"/> on completion.
    /// </summary>
    public void ScaleTempo(int lowerIndex, int higherIndex, float multiplier)
    {
        if (!ValidateIndices(lowerIndex, higherIndex, out lowerIndex, out higherIndex))
            return;

        float lowerPosition = (float)TimingPoints[lowerIndex].MeasurePosition!; // Indices have been validated 
        float higherPosition = (float)TimingPoints[higherIndex].MeasurePosition!; // Indices have been validated 

        float getPositionForSelectedPoints(TimingPoint? timingPoint)
        {
            if (timingPoint?.MeasurePosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            float oldPositionDifference = (float)(timingPoint.MeasurePosition - lowerPosition);
            float newPositionDifference = oldPositionDifference * multiplier;
            return lowerPosition + newPositionDifference;
        }

        float positionOffsetForSubsequentPoints = 0;
        bool areThereAnySubsequentTimingPoints = (higherIndex + 1 < TimingPoints.Count);
        if (areThereAnySubsequentTimingPoints)
        {
            float spanForChangedPointsBefore = (float)(TimingPoints[higherIndex + 1].MeasurePosition - lowerPosition)!;
            float spanForChangedPointsAfter = spanForChangedPointsBefore * multiplier;
            positionOffsetForSubsequentPoints = spanForChangedPointsAfter - spanForChangedPointsBefore;
        }

        float getPositionForSubsequentPoints(TimingPoint? timingPoint)
        {
            if (timingPoint?.MeasurePosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            return (float)(timingPoint.MeasurePosition + positionOffsetForSubsequentPoints);
        }

        if (multiplier > 1)
            BatchChangeMeasurePosition(higherIndex + 1, TimingPoints.Count - 1, getPositionForSubsequentPoints);

        BatchChangeMeasurePosition(lowerIndex, higherIndex, getPositionForSelectedPoints);

        if (multiplier <= 1)
            BatchChangeMeasurePosition(higherIndex + 1, TimingPoints.Count - 1, getPositionForSubsequentPoints);

        if (!areThereAnySubsequentTimingPoints)
        {
            TimingPoint higherTimingPoint = TimingPoints[higherIndex];
            higherTimingPoint.Bpm = higherTimingPoint.Bpm * multiplier;
        }

        MementoHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    /// Scales the tempo of the <see cref="TimingPoint"/>. 
    /// If it is part of a selection, the whole selection's tempo will be scaled.
    /// </summary>
    /// <param name="timingPoint"></param>
    /// <param name="multiplier"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void ScaleTempo(TimingPoint? timingPoint, float multiplier)
    {
        if (timingPoint == null)
            throw new ArgumentNullException();
        int index = TimingPoints.IndexOf(timingPoint);
        if (TimingPointSelection.Instance.Count > 1 && TimingPointSelection.Instance.IsPointInSelection(timingPoint))
        {
            int lower = (int)TimingPointSelection.Instance.SelectionIndices?[0]!; // Can't be null if Count > 1
            int higher = (int)TimingPointSelection.Instance.SelectionIndices?[1]!; // Can't be null if Count > 1

            ScaleTempo(lower, higher, multiplier);
            return;
        }
        ScaleTempo(index, index, multiplier);
    }

    public void BatchChangeOffset(int lowerIndex, int higherIndex, float offsetChange)
    {
        if (!ValidateIndices(lowerIndex, higherIndex, out lowerIndex, out higherIndex))
            return;

        IsBatchOperationInProgress = true;

        bool increasing = offsetChange > 0;

        // If the change decreases the offset, iterate forwards (less likely to trigger rejections).
        // Vice versa for increases
        int startIndex = increasing ? higherIndex : lowerIndex;
        for (int i = startIndex
            ; increasing ? i >= lowerIndex : i <= higherIndex
            ; i = increasing ? i - 1 : i + 1)
        {
            TimingPoint? timingPoint = TimingPoints[i];
            if (timingPoint?.Offset == null)
                throw new NullReferenceException(nameof(timingPoint));

            timingPoint.Offset = timingPoint.Offset + offsetChange;
        }

        IsBatchOperationInProgress = false;

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
    }

    /// <summary>
    /// Ensures that the specified indices can be applied to the <see cref="TimingPoints"/>.
    /// This also means ensuring higherIndex is less than <see cref="TimingPoints"/>.Count.
    /// </summary>
    /// <returns>Whether out parameters are valid</returns>
    private bool ValidateIndices(int lowerIndex, int higherIndex, out int lowerIndexNew, out int higherIndexNew)
    {
        lowerIndexNew = lowerIndex;
        higherIndexNew = higherIndex;

        if (TimingPoints.Count <= 0)
            return false;

        if (lowerIndexNew > higherIndexNew)
            (lowerIndexNew, higherIndexNew) = (higherIndexNew, lowerIndexNew);

        if (higherIndexNew >= TimingPoints.Count)
            higherIndexNew = TimingPoints.Count - 1;

        if (lowerIndexNew < 0)
            lowerIndexNew = 0;

        return true;
    }

    //[Obsolete]
    ///// <summary>
    ///// Can be used after a batch operation since the normal method to update them was blocked by <see cref="IsBatchOperatingInProgress"/>
    ///// </summary>
    //private void UpdateAllTimingPointsMPS()
    //{
    //    foreach (TimingPoint timingPoint in TimingPoints)
    //    {
    //        timingPoint.MeasuresPerSecond_Set(this);
    //    }
    //}
}
