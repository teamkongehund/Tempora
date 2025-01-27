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
        float? position = point?.MusicPosition;
        if (position == null) return false;
        return position == SnapMusicPosition((float)position);
    }

    public int[] GetTimeSignature(float musicPosition)
    {
        //TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
        //if (timingPoint == null) return new int[] { 4, 4 };
        //else return timingPoint.TimeSignature;

        TimeSignaturePoint? timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.MusicPosition <= musicPosition);
        return timeSignaturePoint == null ? ([4, 4]) : timeSignaturePoint.TimeSignature;
    }
    public float SampleTimeToMusicPosition(float time)
    {
        TimingPoint? operatingTimingPoint = GetOperatingTimingPoint_ByTime(time);

        if (operatingTimingPoint?.MusicPosition == null)
        {
            return time * 0.5f; // default 120 bpm from musicposition origin
        }
        else
            return (float)(((time - operatingTimingPoint.Offset) * operatingTimingPoint.MeasuresPerSecond) + operatingTimingPoint.MusicPosition);
    }


    /// <summary>
    /// Get the music-position length of a quarter-note
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public float GetDistancePerBeat(float musicPosition)
    {
        int[] timeSignature = GetTimeSignature(musicPosition);
        return timeSignature[1] / 4f / timeSignature[0];
    }

    /// <summary>
    ///     Returns the music position of the beat at or right before the given music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public float GetOperatingBeatPosition(float musicPosition)
    {
        float beatLength = GetDistancePerBeat(musicPosition);
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        float relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int beatsFromDownbeat = (int)(relativePosition / beatLength);
        float position = (beatsFromDownbeat * beatLength) + downbeatPosition;
        return position;
    }

    /// <summary>
    ///     Returns the music position of the beat at or right after the given music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public float GetNextOperatingBeatPosition(float musicPosition)
    {
        float beatLength = GetDistancePerBeat(musicPosition);
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        float relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int beatsFromDownbeat = (int)MathF.Ceiling(relativePosition / beatLength);

        float position = (relativePosition + beatLength) <= 1
            ? (beatsFromDownbeat * beatLength) + downbeatPosition
            : downbeatPosition + 1;
        return position;
    }

    public float GetNextOperatingGridPosition(float musicPosition)
    {
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        int[] timeSignature = GetTimeSignature(musicPosition);
        int divisor = Settings.Instance.GridDivisor;
        float divisionLength = GetRelativeNotePosition(timeSignature, divisor, 1);
        float relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int divisionsFromDownbeat = (int)MathF.Ceiling(relativePosition / divisionLength);

        float position = (relativePosition + divisionLength) <= 1
            ? (divisionsFromDownbeat * divisionLength) + downbeatPosition
            : downbeatPosition + 1;
        return position;
    }

    public float GetOperatingGridPosition(float musicPosition)
    {
        //TimingPoint? operatingTimingPoint = 
        //    GetOperatingTimingPoint_ByMusicPosition(musicPosition) 
        //    ?? throw new NullReferenceException("This doesn't work unless there's a timing point yet. Fix me so it works always.");
        //int[] timeSignature = operatingTimingPoint.TimeSignature;

        int[] timeSignature = GetTimeSignature(musicPosition);
        int gridDivisor = Settings.Instance.GridDivisor;

        int nextMeasure = (int)(musicPosition + 1);
        float previousAbsolutePosition = GetRelativeNotePosition(timeSignature, gridDivisor, 0) + (int)musicPosition;
        for (int index = 0; index < 30; index++)
        {
            float relativePosition = GetRelativeNotePosition(timeSignature, gridDivisor, index);
            float absolutePosition = (int)musicPosition + relativePosition;

            if (absolutePosition > musicPosition)
                return previousAbsolutePosition;

            if (absolutePosition >= nextMeasure)
                throw new Exception("No operating grid position found");


            previousAbsolutePosition = absolutePosition;
        }

        return 0;
    }

    //public float GetNextOperatingGridPosition(float musicPosition)
    //{
    //    //TimingPoint? operatingTimingPoint =
    //    //    GetOperatingTimingPoint_ByMusicPosition(musicPosition)
    //    //    ?? throw new NullReferenceException("This doesn't work unless there's a timing point yet. Fix me so it works always.");
    //    //int[] timeSignature = operatingTimingPoint.TimeSignature;

    //    int[] timeSignature = GetTimeSignature(musicPosition);
    //    int gridDivisor = Settings.Instance.GridDivisor;

    //    int nextMeasure = (int)(musicPosition + 1);
    //    for (int index = 0; index < 30; index++)
    //    {
    //        float relativePosition = GetRelativeNotePosition(timeSignature, gridDivisor, index);
    //        float absolutePosition = (int)musicPosition + relativePosition;

    //        if (absolutePosition > musicPosition)
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

    public float GetNearestGridPosition(float musicPosition)
    {
        float previous = GetOperatingGridPosition(musicPosition);
        float next = GetNextOperatingGridPosition(musicPosition);
        float distanceToPrevious = Math.Abs(musicPosition - previous);
        float distanceToNext = Math.Abs(next - musicPosition);
        return (distanceToPrevious < distanceToNext) ? previous : next;
    }

    public int GetLastMeasure()
    {
        float lengthInSeconds = Project.Instance.AudioFile?.GetAudioLength() ?? 0;
        float lastMeasure = SampleTimeToMusicPosition(lengthInSeconds);
        return (int)lastMeasure;
    }

    private float GetBeatsBetweenMusicPositions(float musicPositionFrom, float musicPositionTo)
    {
        return GetBeatsBetweenMusicPositions(this, musicPositionFrom, musicPositionTo);
    }

    /// <summary>
    /// Static version to make the method safer to use while the timing is being changed. 
    /// I.e. clone the timing first and parse it as <see cref="timing"/> to use the method without worrying that the timing has changed meanwhile.
    /// </summary>
    /// <returns></returns>
    public static float GetBeatsBetweenMusicPositions(Timing timing, float musicPositionFrom, float musicPositionTo)
    {
        if (musicPositionFrom > musicPositionTo)
            (musicPositionTo, musicPositionFrom) = (musicPositionFrom, musicPositionTo);

        float sum = 0;
        float currentMusicPosition = musicPositionFrom;
        // Go through every measure between them and add up the number of beats
        for (int measure = (int)musicPositionFrom; measure < (int)(musicPositionTo + 1); measure++)
        {
            float distancePerBeat = timing.GetDistancePerBeat(measure);
            float nextPosition = (musicPositionTo < measure + 1) ? musicPositionTo : (measure + 1);
            float distanceToNextPosition = nextPosition - currentMusicPosition;
            float beatsToNextPosition = distanceToNextPosition / distancePerBeat;
            sum += beatsToNextPosition;

            if (nextPosition == musicPositionTo)
                break;

            currentMusicPosition = nextPosition;
        }

        return sum;
    }

    private float GetMusicPositionAfterAddingBeats(float musicPosition, float numberOfBeats)
    {
        return GetMusicPositionAfterAddingBeats(this, musicPosition, numberOfBeats);
    }

    private static float GetMusicPositionAfterAddingBeats(Timing timing, float musicPositionFrom, float numberOfBeatsToAdd)
    {
        if (numberOfBeatsToAdd == 0)
            return musicPositionFrom;

        int lastMeasure = timing.GetLastMeasure();

        float currentMusicPosition = musicPositionFrom;
        float beatsLeftToAdd = numberOfBeatsToAdd;

        bool isAdding = numberOfBeatsToAdd > 0;

        int measure = (int) musicPositionFrom;
        int iteration = 0;

        while (isAdding ? beatsLeftToAdd > 0 : beatsLeftToAdd < 0) 
        {
            float distancePerBeat = timing.GetDistancePerBeat(measure);
            int nextMeasure = (measure = isAdding ? measure + 1 : measure - 1);
            float distanceToNextMeasure = nextMeasure - currentMusicPosition;
            float beatsToNextPosition = distanceToNextMeasure / distancePerBeat;

            if (Math.Abs(beatsLeftToAdd) <= Math.Abs(beatsToNextPosition))
            {
                currentMusicPosition += beatsLeftToAdd * distancePerBeat;
                break;
            }

            beatsLeftToAdd -= beatsToNextPosition;
            currentMusicPosition += distanceToNextMeasure;

            iteration++;
            if (iteration > 10000)
                throw new OverflowException("Too many iterations!");
        }

        return currentMusicPosition;
    }

    public float SnapMusicPosition(float musicPosition)
    {
        if (!Settings.Instance.SnapToGridEnabled)
            return musicPosition;

        return GetNearestGridPosition(musicPosition);
    }
}