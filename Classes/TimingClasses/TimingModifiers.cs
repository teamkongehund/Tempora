using System;
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
    /// <param name="musicPosition"></param>
    public void SnapTimingPoint(TimingPoint timingPoint, float musicPosition, out bool success)
    {
        if (timingPoint == null)
        {
            success = false;
            return;
        }

        float snappedMusicPosition = SnapMusicPosition(musicPosition);
        success = timingPoint.MusicPosition_Set(snappedMusicPosition, this);
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

        Timing oldTiming = CopyTiming(this);

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

        if (TimingPoints.Find(point => point.MusicPosition == musicPosition) == null && TimingPoints.Count > 0)
            AddTimingPoint(musicPosition, MusicPositionToSampleTime(musicPosition));

        ShiftTimingPointsUponTimeSignatureChange(oldTiming, timeSignaturePoint);

        MementoHandler.Instance.AddTimingMemento();

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
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
        TimingPoint? operatingTimingPoint = GetOperatingTimingPoint_ByMusicPosition(timeSignaturePoint.MusicPosition);
        if (operatingTimingPoint == null)
            return;

        int opIndex = TimingPoints.IndexOf(operatingTimingPoint);
        if (opIndex == -1)
            return;

        float getNewMusicPosition(TimingPoint? timingPoint)
        {
            if (timingPoint?.MusicPosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            // Get number of beats from time signature change to timingPoint's old musicPosition using previous timing
            // This should be kept constant
            float beatDifference_OldTiming
                = GetBeatsBetweenMusicPositions(oldTiming, timeSignaturePoint.MusicPosition, (float)timingPoint.MusicPosition);

            float beatDifference_NewTiming
                = GetBeatsBetweenMusicPositions(this, timeSignaturePoint.MusicPosition, (float)timingPoint.MusicPosition);

            float beatsToAdd = beatDifference_OldTiming - beatDifference_NewTiming;

            // Get new music position for this timing point
            float newMusicPosition = GetMusicPositionAfterAddingBeats(this, (float)timingPoint.MusicPosition, beatsToAdd);

            return newMusicPosition;
        }

        BatchChangeMusicPosition(opIndex, TimingPoints.Count - 1, getNewMusicPosition);
    }

    /// <summary>
    /// Delegate that defines what a new music position should be for a timing point.
    /// </summary>
    public delegate float GetNewMusicPosition(TimingPoint? timingPoint);

    /// <summary>
    /// Changes the music positions of all <see cref="TimingPoint"/>s from <see cref="TimingPoints"/>[lowerIndex] to and including <see cref="TimingPoints"/>[higherIndex].
    /// </summary>
    /// <param name="getNewMusicPosition">Delegate method used to calculate the new music position</param>
    /// <exception cref="NullReferenceException"></exception>
    public void BatchChangeMusicPosition(int lowerIndex, int higherIndex, GetNewMusicPosition getNewMusicPosition)
    {
        if (!ValidateIndices(lowerIndex, higherIndex, out lowerIndex, out higherIndex))
            return;

        bool willMusicPositionsIncrease = true;
        if (higherIndex >= lowerIndex + 1)
            willMusicPositionsIncrease = getNewMusicPosition(TimingPoints[lowerIndex + 1]) > TimingPoints[lowerIndex + 1].MusicPosition;

        // If the change decreases the music position, iterate forwards (less likely to trigger rejections).
        // Vice versa for increases
        int startIndex = willMusicPositionsIncrease ? higherIndex : lowerIndex;
        for (int i = startIndex
            ; willMusicPositionsIncrease ? i >= lowerIndex : i <= higherIndex
            ; i = willMusicPositionsIncrease ? i - 1 : i + 1)
        {
            TimingPoint? timingPoint = TimingPoints[i];
            if (timingPoint?.MusicPosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            bool isMusicPositionValid = timingPoint.MusicPosition_Set(getNewMusicPosition(timingPoint), this);
            if (!isMusicPositionValid)
            {
                //GD.Print("Music Position change failed. Stopping batch operation.");
                break;
            }
        }
    }

    /// <summary>
    /// Scales Tempo of selected timing points (including higherIndex) by a multiplier. Adds a <see cref="TimingMemento"/> on completion.
    /// </summary>
    public void ScaleTempo(int lowerIndex, int higherIndex, float multiplier)
    {
        if (!ValidateIndices(lowerIndex, higherIndex, out lowerIndex, out higherIndex))
            return;

        float lowerPosition = (float)TimingPoints[lowerIndex].MusicPosition!; // Indices have been validated 
        float higherPosition = (float)TimingPoints[higherIndex].MusicPosition!; // Indices have been validated 

        float getPositionForSelectedPoints(TimingPoint? timingPoint)
        {
            if (timingPoint?.MusicPosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            float oldPositionDifference = (float)(timingPoint.MusicPosition - lowerPosition);
            float newPositionDifference = oldPositionDifference * multiplier;
            return lowerPosition + newPositionDifference;
        }

        float positionOffsetForSubsequentPoints = 0;
        bool areThereAnySubsequentTimingPoints = (higherIndex + 1 < TimingPoints.Count);
        if (areThereAnySubsequentTimingPoints)
        {
            float spanForChangedPointsBefore = (float)(TimingPoints[higherIndex + 1].MusicPosition - lowerPosition)!;
            float spanForChangedPointsAfter = spanForChangedPointsBefore * multiplier;
            positionOffsetForSubsequentPoints = spanForChangedPointsAfter - spanForChangedPointsBefore;
        }

        float getPositionForSubsequentPoints(TimingPoint? timingPoint)
        {
            if (timingPoint?.MusicPosition == null)
                throw new NullReferenceException(nameof(timingPoint));

            return (float)(timingPoint.MusicPosition + positionOffsetForSubsequentPoints);
        }

        if (multiplier > 1)
            BatchChangeMusicPosition(higherIndex + 1, TimingPoints.Count - 1, getPositionForSubsequentPoints);

        BatchChangeMusicPosition(lowerIndex, higherIndex, getPositionForSelectedPoints);

        if (multiplier <= 1)
            BatchChangeMusicPosition(higherIndex + 1, TimingPoints.Count - 1, getPositionForSubsequentPoints);

        if (!areThereAnySubsequentTimingPoints)
        {
            TimingPoint higherTimingPoint = TimingPoints[higherIndex];
            higherTimingPoint.Bpm_Set(higherTimingPoint.Bpm * multiplier, this);
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

            timingPoint.Offset_Set(timingPoint.Offset + offsetChange, this);
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
