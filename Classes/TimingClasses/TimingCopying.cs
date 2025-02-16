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
        if (Settings.Instance.RemovePointsThatChangeNothing)
            newTiming.RemovePointsThatChangeNothing();
        if (Settings.Instance.AddExtraPointsOnDownbeats)
            newTiming.AddExtraPointsOnDownbeats();
        if (Settings.Instance.AddExtraPointsOnQuarterNotes)
            newTiming.AddExtraPointsOnQuarterNotes();
        if (Settings.Instance.MeasureResetsOnUnsupportedTimeSignatures)
            newTiming.AddExtraPointsOnUnsupportedSignatures(audioFile);
        return newTiming;
    }

    public static Timing CloneAndParseForBeatSaber(Timing timing)
    {
        var newTiming = CopyTiming(timing);
        if (Settings.Instance.RemovePointsThatChangeNothing)
            newTiming.RemovePointsThatChangeNothing();
        
        return newTiming;
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
