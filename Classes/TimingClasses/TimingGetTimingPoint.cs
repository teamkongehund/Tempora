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

using System.Linq;
using System;
using GD = Tempora.Classes.DataHelpers.GD;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    public TimingPoint? GetNearestTimingPoint(float musicPosition)
    {
        if (TimingPoints.Count == 0)
            return null;

        TimingPoint? previousTimingPoint = TimingPoints.FindLast(point => point.MusicPosition <= musicPosition);
        TimingPoint? nextTimingPoint = TimingPoints.Find(point => point.MusicPosition > musicPosition);

        TimingPoint? timingPoint = null;

        if (previousTimingPoint?.MusicPosition != null && nextTimingPoint == null)
            return previousTimingPoint;
        else if (previousTimingPoint == null && nextTimingPoint?.MusicPosition != null)
            return nextTimingPoint;
        else if (previousTimingPoint?.MusicPosition != null && nextTimingPoint?.MusicPosition != null)
        {
            float distanceToNext = Math.Abs((float)nextTimingPoint.MusicPosition - musicPosition);
            float distanceToPrevious = Math.Abs((float)previousTimingPoint.MusicPosition - musicPosition);
            timingPoint = (distanceToPrevious < distanceToNext) ? previousTimingPoint : nextTimingPoint;
        }

        return timingPoint == null
            ? throw new NullReferenceException("Timing point does not exist")
            : timingPoint.MusicPosition == null
            ? throw new NullReferenceException($"Nearest TimingPoint does not have a non-null {nameof(TimingPoint.MusicPosition)}")
            : timingPoint;
    }

    public TimingPoint? GetOperatingTimingPoint_ByMusicPosition(float musicPosition)
    {
        if (TimingPoints.Count == 0)
            return null;

        TimingPoint? timingPoint = TimingPoints.FindLast(point => point.MusicPosition <= musicPosition);

        // If there's only TimingPoints AFTER MusicPositionStart
        timingPoint ??= TimingPoints.Find(point => point.MusicPosition > musicPosition);

        return timingPoint == null
            ? throw new NullReferenceException("Timing point does not exist")
            : timingPoint.MusicPosition == null
            ? throw new NullReferenceException($"Operating TimingPoint does not have a non-null {nameof(TimingPoint.MusicPosition)}")
            : timingPoint;
    }

    public TimingPoint? GetOperatingTimingPoint_ByTime(float time)
    {
        // Ensures the method can be used while a TimingPoint is being created.
        var validTimingPoints = TimingPoints.Where(point => point.MusicPosition != null).ToList<TimingPoint>();

        if (validTimingPoints == null)
            return null;

        int operatingTimingPointIndex = validTimingPoints.FindLastIndex(point => point.Offset <= time);
        TimingPoint? operatingTimingPoint = operatingTimingPointIndex == -1 ? TimingPoints.Find(point => point.Offset > time) : validTimingPoints[operatingTimingPointIndex];

        return operatingTimingPoint;
    }

    public TimingPoint? GetPreviousTimingPoint(TimingPoint? timingPoint)
    {
        if (timingPoint == null)
            return null;

        int i = timingPoints.IndexOf(timingPoint);

        if (i == -1)
            GD.Print($"PreviousTimingPoint(): Timing point {timingPoint} with index {i} (taken from Timing {this}) is not present in the list of timing points");
        return i - 1 < 0 ? null : timingPoints[i - 1];
    }

    public TimingPoint? GetNextTimingPoint(TimingPoint? timingPoint)
    {
        if (timingPoint == null)
            return null;

        int i = timingPoints.IndexOf(timingPoint);

        if (i == -1)
            throw new Exception($"NextTimingPoint(): Timing point {timingPoint} with index {i} (taken from Timing {this}) is not present in the list of timing points");
        return i + 1 >= timingPoints.Count ? null : timingPoints[i + 1];
    }
}
