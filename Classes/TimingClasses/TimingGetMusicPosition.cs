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
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

public partial class Timing
{
    public bool IsTimingPointOnGrid(TimingPoint? point)
    {
        float? position = point?.MeasurePosition;
        if (position == null) return false;
        return position == SnapMeasurePosition((float)position);
    }

    public int[] GetTimeSignature(float measurePosition)
    {
        //TimingPoint timingPoint = GetOperatingTimingPoint(measurePosition);
        //if (timingPoint == null) return new int[] { 4, 4 };
        //else return timingPoint.TimeSignature;

        TimeSignaturePoint? timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.Measure <= measurePosition);
        return timeSignaturePoint == null ? ([4, 4]) : timeSignaturePoint.TimeSignature;
    }
    public float SampleTimeToMeasurePosition(float time)
    {
        TimingPoint? operatingTimingPoint = GetOperatingTimingPoint_ByTime(time);

        if (operatingTimingPoint?.MeasurePosition == null)
        {
            return time * 0.5f; // default 120 bpm from musicposition origin
        }
        else
            return (float)(((time - operatingTimingPoint.Offset) * operatingTimingPoint.MeasuresPerSecond) + operatingTimingPoint.MeasurePosition);
    }


    /// <summary>
    /// Get the music-position length of a quarter-note
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <returns></returns>
    public float GetDistancePerBeat(float measurePosition)
    {
        int[] timeSignature = GetTimeSignature(measurePosition);
        return timeSignature[1] / 4f / timeSignature[0];
    }

    /// <summary>
    ///     Returns the music position of the beat at or right before the given music position.
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <returns></returns>
    public float GetOperatingBeatPosition(float measurePosition)
    {
        float beatLength = GetDistancePerBeat(measurePosition);
        int downbeatPosition = (measurePosition >= 0) ? (int)measurePosition : (int)measurePosition - 1;
        float relativePosition = (measurePosition >= 0)
            ? measurePosition % 1
            : 1 + (measurePosition % 1);
        int beatsFromDownbeat = (int)(relativePosition / beatLength);
        float position = (beatsFromDownbeat * beatLength) + downbeatPosition;
        return position;
    }

    /// <summary>
    ///     Returns the music position of the beat at or right after the given music position.
    /// </summary>
    /// <param name="measurePosition"></param>
    /// <returns></returns>
    public float GetNextOperatingBeatPosition(float measurePosition)
    {
        float beatLength = GetDistancePerBeat(measurePosition);
        int downbeatPosition = (measurePosition >= 0) ? (int)measurePosition : (int)measurePosition - 1;
        float relativePosition = (measurePosition >= 0)
            ? measurePosition % 1
            : 1 + (measurePosition % 1);
        int beatsFromDownbeat = (int)MathF.Ceiling(relativePosition / beatLength);

        float position = (relativePosition + beatLength) <= 1
            ? (beatsFromDownbeat * beatLength) + downbeatPosition
            : downbeatPosition + 1;
        return position;
    }

    public float GetNextOperatingGridPosition(float measurePosition)
    {
        int downbeatPosition = (measurePosition >= 0) ? (int)measurePosition : (int)measurePosition - 1;
        int[] timeSignature = GetTimeSignature(measurePosition);
        int divisor = Settings.Instance.GridDivisor;
        float divisionLength = GetRelativeNotePosition(timeSignature, divisor, 1);
        float relativePosition = (measurePosition >= 0)
            ? measurePosition % 1
            : 1 + (measurePosition % 1);
        int divisionsFromDownbeat = (int)MathF.Ceiling(relativePosition / divisionLength);

        float position = (relativePosition + divisionLength) <= 1
            ? (divisionsFromDownbeat * divisionLength) + downbeatPosition
            : downbeatPosition + 1;
        return position;
    }

    public float GetOperatingGridPosition(float measurePosition)
    {
        //TimingPoint? operatingTimingPoint = 
        //    GetOperatingTimingPoint_ByMeasurePosition(measurePosition) 
        //    ?? throw new NullReferenceException("This doesn't work unless there's a timing point yet. Fix me so it works always.");
        //int[] timeSignature = operatingTimingPoint.TimeSignature;

        int[] timeSignature = GetTimeSignature(measurePosition);
        int gridDivisor = Settings.Instance.GridDivisor;

        int nextMeasure = (int)(measurePosition + 1);
        float previousAbsolutePosition = GetRelativeNotePosition(timeSignature, gridDivisor, 0) + (int)measurePosition;
        for (int index = 0; index < 30; index++)
        {
            float relativePosition = GetRelativeNotePosition(timeSignature, gridDivisor, index);
            float absolutePosition = (int)measurePosition + relativePosition;

            if (absolutePosition > measurePosition)
                return previousAbsolutePosition;

            if (absolutePosition >= nextMeasure)
                throw new Exception("No operating grid position found");


            previousAbsolutePosition = absolutePosition;
        }

        return 0;
    }

    //public float GetNextOperatingGridPosition(float measurePosition)
    //{
    //    //TimingPoint? operatingTimingPoint =
    //    //    GetOperatingTimingPoint_ByMeasurePosition(measurePosition)
    //    //    ?? throw new NullReferenceException("This doesn't work unless there's a timing point yet. Fix me so it works always.");
    //    //int[] timeSignature = operatingTimingPoint.TimeSignature;

    //    int[] timeSignature = GetTimeSignature(measurePosition);
    //    int gridDivisor = Settings.Instance.GridDivisor;

    //    int nextMeasure = (int)(measurePosition + 1);
    //    for (int index = 0; index < 30; index++)
    //    {
    //        float relativePosition = GetRelativeNotePosition(timeSignature, gridDivisor, index);
    //        float absolutePosition = (int)measurePosition + relativePosition;

    //        if (absolutePosition > measurePosition)
    //            return absolutePosition;

    //        if (absolutePosition >= nextMeasure)
    //            throw new Exception("No operating grid position found");
    //    }

    //    return 0;
    //}

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

    public float GetNearestGridPosition(float measurePosition)
    {
        float previous = GetOperatingGridPosition(measurePosition);
        float next = GetNextOperatingGridPosition(measurePosition);
        float distanceToPrevious = Math.Abs(measurePosition - previous);
        float distanceToNext = Math.Abs(next - measurePosition);
        return (distanceToPrevious < distanceToNext) ? previous : next;
    }

    public int GetLastMeasure()
    {
        float lengthInSeconds = Project.Instance.AudioFile?.GetAudioLength() ?? 0;
        float lastMeasure = SampleTimeToMeasurePosition(lengthInSeconds);
        return (int)lastMeasure;
    }

    private float GetBeatsBetweenMeasurePositions(float measurePositionFrom, float measurePositionTo)
    {
        return GetBeatsBetweenMeasurePositions(this, measurePositionFrom, measurePositionTo);
    }

    /// <summary>
    /// Static version to make the method safer to use while the timing is being changed. 
    /// I.e. clone the timing first and parse it as <see cref="timing"/> to use the method without worrying that the timing has changed meanwhile.
    /// </summary>
    /// <returns></returns>
    public static float GetBeatsBetweenMeasurePositions(Timing timing, float measurePositionFrom, float measurePositionTo)
    {
        if (measurePositionFrom > measurePositionTo)
            (measurePositionTo, measurePositionFrom) = (measurePositionFrom, measurePositionTo);

        float sum = 0;
        float currentMeasurePosition = measurePositionFrom;
        // Go through every measure between them and add up the number of beats
        for (int measure = (int)measurePositionFrom; measure < (int)(measurePositionTo + 1); measure++)
        {
            float distancePerBeat = timing.GetDistancePerBeat(measure);
            float nextPosition = (measurePositionTo < measure + 1) ? measurePositionTo : (measure + 1);
            float distanceToNextPosition = nextPosition - currentMeasurePosition;
            float beatsToNextPosition = distanceToNextPosition / distancePerBeat;
            sum += beatsToNextPosition;

            if (nextPosition == measurePositionTo)
                break;

            currentMeasurePosition = nextPosition;
        }

        return sum;
    }

    private float GetMeasurePositionAfterAddingBeats(float measurePosition, float numberOfBeats)
    {
        return GetMeasurePositionAfterAddingBeats(this, measurePosition, numberOfBeats);
    }

    private static float GetMeasurePositionAfterAddingBeats(Timing timing, float measurePositionFrom, float numberOfBeatsToAdd)
    {
        if (numberOfBeatsToAdd == 0)
            return measurePositionFrom;

        int lastMeasure = timing.GetLastMeasure();

        float currentMeasurePosition = measurePositionFrom;
        float beatsLeftToAdd = numberOfBeatsToAdd;

        bool isAdding = numberOfBeatsToAdd > 0;

        int measure = (int) measurePositionFrom;
        int iteration = 0;

        // Iterate measures
        while (isAdding ? beatsLeftToAdd > 0 : beatsLeftToAdd < 0)
        {
            bool usePreviousMeasure = !isAdding && (int)currentMeasurePosition == currentMeasurePosition;

            float signedDistanceToNextDownbeat = isAdding 
                ? (int)(currentMeasurePosition + 1) - currentMeasurePosition
                : usePreviousMeasure
                ? -1
                : -(currentMeasurePosition - (int)currentMeasurePosition);

            var distancePerBeat = timing.GetDistancePerBeat(currentMeasurePosition - (usePreviousMeasure ? 1 : 0));
            var potentialBeatsToAdd = signedDistanceToNextDownbeat / distancePerBeat;

            if (MathF.Abs(potentialBeatsToAdd) < MathF.Abs(beatsLeftToAdd))
            {
                currentMeasurePosition += potentialBeatsToAdd * distancePerBeat;
                beatsLeftToAdd -= potentialBeatsToAdd;
            }
            else
            {
                currentMeasurePosition += beatsLeftToAdd * distancePerBeat;
                break;
            }

            iteration++;
            if (iteration > 10000)
                throw new OverflowException("Too many iterations!");
        }

        return currentMeasurePosition;
    }

    public float SnapMeasurePosition(float measurePosition)
    {
        if (!Settings.Instance.SnapToGridEnabled)
            return measurePosition;

        return GetNearestGridPosition(measurePosition);
    }
}