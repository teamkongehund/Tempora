// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share — copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution — You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial — You may not use the material for commercial purposes.
// - NoDerivatives — If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System.Collections.Generic;
using System;
using Tempora.Classes.DataHelpers;
using Tempora.Classes.Utility;
using Tempora.Classes.Audio;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    public static Timing CloneAndParseForOsu(Timing timing, AudioFile audioFile)
    {
        var newTiming = CopyTiming(timing);
        RemovePointsThatChangeNothing(newTiming, out newTiming);
        AddExtraPointsOnDownbeats(newTiming, out newTiming);
        AddExtraPointsOnQuarterNotes(newTiming, out newTiming);
        AddExtraPointsOn8thSignatures(newTiming, out newTiming, audioFile);
        return newTiming;
    }

    private static void AddExtraPointsOnDownbeats(Timing timing, out Timing newTiming)
    {
        newTiming = timing;
        var downbeatPositions = new List<int>();
        foreach (TimingPoint timingPoint in newTiming.TimingPoints)
        {
            if (timingPoint?.MusicPosition == null)
                break;
            if (timingPoint.MusicPosition % 1 == 0)
                continue; // downbeat point on next is unnecessary
            TimingPoint? nextTimingPoint = newTiming.GetNextTimingPoint(timingPoint);
            bool isNextPointInSameMeasure = nextTimingPoint?.MusicPosition != null 
                && (int)nextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition;
            bool isThereAPointOnNextDownbeat = nextTimingPoint?.MusicPosition != null
                && nextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition + 1;
            if (isNextPointInSameMeasure || isThereAPointOnNextDownbeat)
                continue;
            downbeatPositions.Add((int)timingPoint.MusicPosition + 1);
        }
        foreach (int downbeat in downbeatPositions)
        {
            //float time = newTiming.MusicPositionToTime(downbeat);
            //newTiming.AddTimingPoint(downbeat, time);
            newTiming.AddTimingPoint(downbeat);
        }
    }

    private static void AddExtraPointsOnQuarterNotes(Timing timing, out Timing newTiming)
    {
        newTiming = timing;
        // Add extra quarter-note timing points
        var quaterNotePositions = new List<float>();
        foreach (TimingPoint timingPoint in newTiming.TimingPoints)
        {
            if (timingPoint == null)
                break;
            if (timingPoint.MusicPosition == null)
                break;

            float beatLength = newTiming.GetDistancePerBeat((float)timingPoint.MusicPosition);
            float beatPosition = newTiming.GetOperatingBeatPosition((float)timingPoint.MusicPosition);
            TimingPoint? nextTimingPoint = newTiming.GetNextTimingPoint(timingPoint);
            float? nextPointPosition = nextTimingPoint?.MusicPosition;

            //float epsilon = 0.00001f;
            //bool isOnQuarterNote = (timingPoint.MusicPosition % beatLength < epsilon || (beatLength - timingPoint.MusicPosition % beatLength) < epsilon);

            bool isOnQuarterNote = IsPositionOnDivisor((float)timingPoint.MusicPosition, timingPoint.TimeSignature, 4);

            bool nextPointIsOnOrBeforeNextQuarterNote = (nextTimingPoint != null
                && nextPointPosition <= beatPosition + beatLength);
            if (isOnQuarterNote || nextPointIsOnOrBeforeNextQuarterNote)
                continue;

            quaterNotePositions.Add(beatPosition + beatLength);
        }
        foreach (float quarterNote in quaterNotePositions)
        {
            //float time = newTiming.MusicPositionToTime(quarterNote);
            //newTiming.AddTimingPoint(quarterNote, time);
            newTiming.AddTimingPoint(quarterNote);
        }
    }

    private static void AddExtraPointsOn8thSignatures(Timing timing, out Timing newTiming, AudioFile audioFile)
    {
        newTiming = timing;

        // Maybe add exceptions later like 4/8 and 8/8 

        int firstMeasure = (int)newTiming.SampleTimeToMusicPosition(0f);
        int lastMeasure = (int)newTiming.SampleTimeToMusicPosition((float)audioFile.Stream.GetLength());

        for (int measure = firstMeasure;  measure < lastMeasure + 1; measure++)
        {
            int[] timeSignature = newTiming.GetTimeSignature(measure);
            if (timeSignature[1] == 4) continue;

            TimingPoint? operatingPoint = newTiming.GetOperatingTimingPoint_ByMusicPosition(measure);

            if (operatingPoint?.MusicPosition == measure)
                continue;

            newTiming.AddTimingPoint(measure);
        }
    }

    private static void RemovePointsThatChangeNothing(Timing timing, out Timing newTiming)
    {
        newTiming = timing;

        var pointsToDelete = new List<TimingPoint>();

        foreach(TimingPoint timingPoint in newTiming.TimingPoints)
        {
            var previous = newTiming.GetPreviousTimingPoint(timingPoint);
            bool mpsIsSame = previous?.MeasuresPerSecond == timingPoint.MeasuresPerSecond;
            bool ts0IsSame = previous?.TimeSignature[0] == timingPoint.TimeSignature[0];
            bool ts1IsSame = previous?.TimeSignature[1] == timingPoint.TimeSignature[1];

            if (mpsIsSame && ts0IsSame && ts1IsSame)
                pointsToDelete.Add(timingPoint);
        }

        foreach(TimingPoint timingPoint in pointsToDelete)
        {
            newTiming.DeleteTimingPoint(timingPoint);
        }
    }

    public static Timing CopyTiming(Timing timing)
    {
        var newTiming = new Timing
        {
            TimingPoints = CloneUtility.CloneList<TimingPoint>(timing.timingPoints),
            TimeSignaturePoints = CloneUtility.CloneList<TimeSignaturePoint>(timing.TimeSignaturePoints)
        };

        return newTiming;
    }

    public IMemento GetMemento()
    {
        return new TimingMemento(this);
    }

    public void RestoreMemento(IMemento memento)
    {
        ArgumentNullException.ThrowIfNull(memento);

        if (memento is not TimingMemento timingMemento)
            throw new ArgumentException($"{nameof(memento)} was not of type {nameof(TimingMemento)}");

        int oldCount = TimingPoints.Count;

        TimingPoints = CloneUtility.CloneList<TimingPoint>(timingMemento.clonedTimingPoints);
        TimeSignaturePoints = CloneUtility.CloneList<TimeSignaturePoint>(timingMemento.clonedTimeSignaturePoints);

        if (oldCount != TimingPoints.Count)
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
    }

    private class TimingMemento(Timing originator) : IMemento
    {
        public readonly List<TimingPoint> clonedTimingPoints = CloneUtility.CloneList(originator.timingPoints);
        public readonly List<TimeSignaturePoint> clonedTimeSignaturePoints = CloneUtility.CloneList(originator.timeSignaturePoints);

        public IMementoOriginator GetOriginator() => originator;
    }
}
