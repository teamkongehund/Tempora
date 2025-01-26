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
        double? position = point?.MusicPosition;
        if (position == null) return false;
        return position == SnapMusicPosition((float)position);
    }

    public int[] GetTimeSignature(double musicPosition)
    {
        //TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
        //if (timingPoint == null) return new int[] { 4, 4 };
        //else return timingPoint.TimeSignature;

        TimeSignaturePoint? timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.MusicPosition <= musicPosition);
        return timeSignaturePoint == null ? ([4, 4]) : timeSignaturePoint.TimeSignature;
    }
    public double SampleTimeToMusicPosition(double time)
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
    public double GetDistancePerBeat(double musicPosition)
    {
        int[] timeSignature = GetTimeSignature(musicPosition);
        return timeSignature[1] / 4f / timeSignature[0];
    }

    /// <summary>
    ///     Returns the music position of the beat at or right before the given music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public double GetOperatingBeatPosition(double musicPosition)
    {
        double beatLength = GetDistancePerBeat(musicPosition);
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        double relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int beatsFromDownbeat = (int)(relativePosition / beatLength);
        double position = (beatsFromDownbeat * beatLength) + downbeatPosition;
        return position;
    }

    /// <summary>
    ///     Returns the music position of the beat at or right after the given music position.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <returns></returns>
    public double GetNextOperatingBeatPosition(double musicPosition)
    {
        double beatLength = GetDistancePerBeat(musicPosition);
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        double relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int beatsFromDownbeat = (int)Math.Ceiling(relativePosition / beatLength);

        double position = (relativePosition + beatLength) <= 1
            ? (beatsFromDownbeat * beatLength) + downbeatPosition
            : downbeatPosition + 1;
        return position;
    }

    public double GetNextOperatingGridPosition(double musicPosition)
    {
        int downbeatPosition = (musicPosition >= 0) ? (int)musicPosition : (int)musicPosition - 1;
        int[] timeSignature = GetTimeSignature(musicPosition);
        int divisor = Settings.Instance.GridDivisor;
        double divisionLength = GetRelativeNotePosition(timeSignature, divisor, 1);
        double relativePosition = (musicPosition >= 0)
            ? musicPosition % 1
            : 1 + (musicPosition % 1);
        int divisionsFromDownbeat = (int)Math.Ceiling(relativePosition / divisionLength);

        double position = (relativePosition + divisionLength) <= 1
            ? (divisionsFromDownbeat * divisionLength) + downbeatPosition
            : downbeatPosition + 1;
        return position;
    }

    public double GetOperatingGridPosition(double musicPosition)
    {
        //TimingPoint? operatingTimingPoint = 
        //    GetOperatingTimingPoint_ByMusicPosition(musicPosition) 
        //    ?? throw new NullReferenceException("This doesn't work unless there's a timing point yet. Fix me so it works always.");
        //int[] timeSignature = operatingTimingPoint.TimeSignature;

        int[] timeSignature = GetTimeSignature(musicPosition);
        int gridDivisor = Settings.Instance.GridDivisor;

        int nextMeasure = (int)(musicPosition + 1);
        double previousAbsolutePosition = GetRelativeNotePosition(timeSignature, gridDivisor, 0) + (int)musicPosition;
        for (int index = 0; index < 30; index++)
        {
            double relativePosition = GetRelativeNotePosition(timeSignature, gridDivisor, index);
            double absolutePosition = (int)musicPosition + relativePosition;

            if (absolutePosition > musicPosition)
                return previousAbsolutePosition;

            if (absolutePosition >= nextMeasure)
                throw new Exception("No operating grid position found");


            previousAbsolutePosition = absolutePosition;
        }

        return 0;
    }

    public static double GetRelativeNotePosition(int[] timeSignature, int gridDivisor, int index)
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

        double position = index * timeSignature[1] / (double)(timeSignature[0] * gridDivisor);
        return position;
    }

    public double GetNearestGridPosition(double musicPosition)
    {
        double previous = GetOperatingGridPosition(musicPosition);
        double next = GetNextOperatingGridPosition(musicPosition);
        double distanceToPrevious = Math.Abs(musicPosition - previous);
        double distanceToNext = Math.Abs(next - musicPosition);
        return (distanceToPrevious < distanceToNext) ? previous : next;
    }

    public int GetLastMeasure()
    {
        double lengthInSeconds = Project.Instance.AudioFile?.GetAudioLength() ?? 0;
        double lastMeasure = SampleTimeToMusicPosition(lengthInSeconds);
        return (int)lastMeasure;
    }

    private double GetBeatsBetweenMusicPositions(double musicPositionFrom, double musicPositionTo)
    {
        return GetBeatsBetweenMusicPositions(this, musicPositionFrom, musicPositionTo);
    }

    /// <summary>
    /// Static version to make the method safer to use while the timing is being changed. 
    /// I.e. clone the timing first and parse it as <see cref="timing"/> to use the method without worrying that the timing has changed meanwhile.
    /// </summary>
    /// <returns></returns>
    private static double GetBeatsBetweenMusicPositions(Timing timing, double musicPositionFrom, double musicPositionTo)
    {
        if (musicPositionFrom > musicPositionTo)
            (musicPositionTo, musicPositionFrom) = (musicPositionFrom, musicPositionTo);

        double sum = 0;
        double currentMusicPosition = musicPositionFrom;
        // Go through every measure between them and add up the number of beats
        for (int measure = (int)musicPositionFrom; measure < (int)(musicPositionTo + 1); measure++)
        {
            double distancePerBeat = timing.GetDistancePerBeat(measure);
            double nextPosition = (musicPositionTo < measure + 1) ? musicPositionTo : (measure + 1);
            double distanceToNextPosition = nextPosition - currentMusicPosition;
            double beatsToNextPosition = distanceToNextPosition / distancePerBeat;
            sum += beatsToNextPosition;

            if (nextPosition == musicPositionTo)
                break;

            currentMusicPosition = nextPosition;
        }

        return sum;
    }

    private double GetMusicPositionAfterAddingBeats(double musicPosition, double numberOfBeats)
    {
        return GetMusicPositionAfterAddingBeats(this, musicPosition, numberOfBeats);
    }

    private static double GetMusicPositionAfterAddingBeats(Timing timing, double musicPositionFrom, double numberOfBeatsToAdd)
    {
        if (numberOfBeatsToAdd == 0)
            return musicPositionFrom;

        int lastMeasure = timing.GetLastMeasure();

        double currentMusicPosition = musicPositionFrom;
        double beatsLeftToAdd = numberOfBeatsToAdd;

        bool isAdding = numberOfBeatsToAdd > 0;

        int measure = (int) musicPositionFrom;
        int iteration = 0;

        while (isAdding ? beatsLeftToAdd > 0 : beatsLeftToAdd < 0) 
        {
            double distancePerBeat = timing.GetDistancePerBeat(measure);
            int nextMeasure = (measure = isAdding ? measure + 1 : measure - 1);
            double distanceToNextMeasure = nextMeasure - currentMusicPosition;
            double beatsToNextPosition = distanceToNextMeasure / distancePerBeat;

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

    public double SnapMusicPosition(double musicPosition)
    {
        if (!Settings.Instance.SnapToGridEnabled)
            return musicPosition;

        return GetNearestGridPosition(musicPosition);
    }
}