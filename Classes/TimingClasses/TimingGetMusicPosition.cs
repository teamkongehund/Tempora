﻿using System;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

public partial class Timing
{
    public int[] GetTimeSignature(float musicPosition)
    {
        //TimingPoint timingPoint = GetOperatingTimingPoint(musicPosition);
        //if (timingPoint == null) return new int[] { 4, 4 };
        //else return timingPoint.TimeSignature;

        TimeSignaturePoint? timeSignaturePoint = TimeSignaturePoints.FindLast(point => point.MusicPosition <= musicPosition);
        return timeSignaturePoint == null ? ([4, 4]) : timeSignaturePoint.TimeSignature;
    }
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

    private float GetBeatsBetweenMusicPositions(float musicPositionFrom, float musicPositionTo)
    {
        return GetBeatsBetweenMusicPositions(this, musicPositionFrom, musicPositionTo);
    }

    /// <summary>
    /// Static version to make the method safer to use while the timing is being changed. 
    /// I.e. clone the timing first and parse it as <see cref="timing"/> to use the method without worrying that the timing has changed meanwhile.
    /// </summary>
    /// <returns></returns>
    private static float GetBeatsBetweenMusicPositions(Timing timing, float musicPositionFrom, float musicPositionTo)
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
            currentMusicPosition = isAdding ? currentMusicPosition + 1 : currentMusicPosition - 1;

            iteration++;
            if (iteration > 10000)
                throw new OverflowException("Too many iterations!");
        }

        return currentMusicPosition;
    }

    public static float SnapMusicPosition(float musicPosition)
    {
        if (!Settings.Instance.SnapToGridEnabled)
            return musicPosition;

        int divisor = Settings.Instance.GridDivisor;
        //float divisionLength = 1f / divisor;
        float divisionLength = GetRelativeNotePosition(Instance.GetTimeSignature(musicPosition), divisor, 1);

        float relativePosition = musicPosition - (int)musicPosition;

        int divisionIndex = (int)Math.Round(relativePosition / divisionLength);

        float snappedMusicPosition = (int)musicPosition + (divisionIndex * divisionLength);

        return snappedMusicPosition;
    }
}