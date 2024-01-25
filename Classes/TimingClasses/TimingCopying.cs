using System.Collections.Generic;
using System;
using Tempora.Classes.DataTools;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    public static Timing CopyAndAddExtraPoints(Timing timing)
    {
        var newTiming = new Timing
        {
            TimingPoints = CloneUtility.CloneList<TimingPoint>(timing.timingPoints),
            TimeSignaturePoints = CloneUtility.CloneList<TimeSignaturePoint>(timing.TimeSignaturePoints)
        };

        // Add extra downbeat timing points
        var downbeatPositions = new List<int>();
        foreach (TimingPoint timingPoint in timing.TimingPoints)
        {
            if (timingPoint?.MusicPosition == null)
                break;
            //if (timingPoint.NextTimingPoint == null) break;
            if (timingPoint.MusicPosition % 1 == 0)
                continue; // downbeat point on next is unnecessary
            //if (timingPoint.NextTimingPoint != null && (int)(timingPoint?.NextTimingPoint.MusicPosition) == (int)timingPoint.MusicPosition) continue; // next timing point is in same measure
            //if (timingPoint.NextTimingPoint != null && timingPoint.NextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition + 1) continue; // downbeat point on next measure already exists
            TimingPoint? nextTimingPoint = Timing.Instance.GetNextTimingPoint(timingPoint);
            if (nextTimingPoint?.MusicPosition != null && (int)nextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition)
                continue; // next timing point is in same measure
            if (nextTimingPoint?.MusicPosition != null && nextTimingPoint.MusicPosition == (int)timingPoint.MusicPosition + 1)
                continue; // downbeat point on next measure already exists
            downbeatPositions.Add((int)timingPoint.MusicPosition + 1);
        }
        foreach (int downbeat in downbeatPositions)
        {
            float time = newTiming.MusicPositionToTime(downbeat);
            newTiming.AddTimingPoint(downbeat, time);
        }

        // Add extra quarter-note timing points
        var quaterNotePositions = new List<float>();
        foreach (TimingPoint timingPoint in newTiming.TimingPoints)
        {
            if (timingPoint == null)
                break;
            //if (timingPoint.NextTimingPoint == null) break;
            if (timingPoint.MusicPosition == null)
                break;

            float beatLengthMP = timing.GetDistancePerBeat((float)timingPoint.MusicPosition);
            float beatPosition = timing.GetOperatingBeatPosition((float)timingPoint.MusicPosition);
            //float? nextPointPosition = timingPoint?.NextTimingPoint?.MusicPosition;
            TimingPoint? nextTimingPoint = Timing.Instance.GetNextTimingPoint(timingPoint);
            float? nextPointPosition = nextTimingPoint?.MusicPosition;

            if (timingPoint.MusicPosition % beatLengthMP == 0)
                continue; // is on quarter-note 
            //if (timingPoint.NextTimingPoint != null 
            //    && nextPointPosition <= beatPosition + beatLengthMP) 
            //    continue; // next timing point is on or before next quarter-note
            if (nextTimingPoint != null
                && nextPointPosition <= beatPosition + beatLengthMP)
            {
                continue; // next timing point is on or before next quarter-note
            }

            quaterNotePositions.Add(beatPosition + beatLengthMP);
        }
        foreach (float quarterNote in quaterNotePositions)
        {
            float time = newTiming.MusicPositionToTime(quarterNote);
            newTiming.AddTimingPoint(quarterNote, time);
        }

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

        TimingPoints = CloneUtility.CloneList<TimingPoint>(timingMemento.clonedTimingPoints);
        TimeSignaturePoints = CloneUtility.CloneList<TimeSignaturePoint>(timingMemento.clonedTimeSignaturePoints);
    }

    private class TimingMemento(Timing originator) : IMemento
    {
        public readonly List<TimingPoint> clonedTimingPoints = CloneUtility.CloneList(originator.timingPoints);
        public readonly List<TimeSignaturePoint> clonedTimeSignaturePoints = CloneUtility.CloneList(originator.timeSignaturePoints);

        public IMementoOriginator GetOriginator() => originator;
    }
}
