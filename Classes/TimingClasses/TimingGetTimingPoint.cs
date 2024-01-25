using System.Linq;
using System;
using GD = Tempora.Classes.DataTools.GD;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
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
            GD.Print("Timing point is not present in the list of timing points");
        return i - 1 < 0 ? null : timingPoints[i - 1];
    }

    public TimingPoint? GetNextTimingPoint(TimingPoint? timingPoint)
    {
        if (timingPoint == null)
            return null;

        int i = timingPoints.IndexOf(timingPoint);

        if (i == -1)
            GD.Print("Timing point is not present in the list of timing points");
        return i + 1 >= timingPoints.Count ? null : timingPoints[i + 1];
    }
}
