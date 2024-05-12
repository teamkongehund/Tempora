using System.Collections.Generic;
using System;
using Tempora.Classes.DataHelpers;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    public static Timing CopyAndAddExtraPoints(Timing timing)
    {
        var newTiming = CopyTiming(timing);
        AddExtraPointsOnDownbeats(newTiming, out newTiming);
        AddExtraPointsOnQuarterNotes(newTiming, out newTiming);
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

            float epsilon = 0.00001f;
            bool isOnQuarterNote = (timingPoint.MusicPosition % beatLength < epsilon || (beatLength - timingPoint.MusicPosition % beatLength) < epsilon);
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
