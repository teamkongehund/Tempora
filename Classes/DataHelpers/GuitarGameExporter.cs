using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;

namespace Tempora.Classes.DataHelpers;
public class GuitarGameExporter
{
    private static int ticksPerBeat = 192;

    private static string syncTrackSectionUnFormatted = @"[SyncTrack]
{
{0}
}";

    private string? GetSyncTrackSection(Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);

        string syncTrackSectionInner = "";

        if (timing.TimingPoints.Count == 0)
            return string.Format(syncTrackSectionUnFormatted, GetBpmLine(0, 120));

        float measurePositionAtSampleZero = timing.OffsetToMeasurePosition(0);

        float secondLineBeatsFromSampleZero;
        int ticksOfSecondLine;
        float bpmOfFirstLine;
        float offsetOfSecondLine;
        string firstTimeSignatureLine;
        string firstBpmLine;

        // To get the first SyncTrack line, we need to use the first timing point with an offset that is later than 0, taking floating point error into account
        // We get the number of beats from offset zero to this timing point. We use the difference in offset in combination with this to get the BPM.

        TimingPoint? timingPointCreatingSecondLine = timing.TimingPoints.Find(
            point => !Timing.AreMeasurePositionsEqual(measurePositionAtSampleZero, point.MeasurePosition) && point.Offset > 0);

        if (timingPointCreatingSecondLine == null) // Only exist points at negative offset
        {
            TimingPoint? lastTimingPoint = timing.TimingPoints.LastOrDefault() ?? throw new Exception("No timing points.");

            float quarterNoteLength = timing.GetDistancePerBeat(measurePositionAtSampleZero);
            float positionOfFirstQuarterNotePlusRemainder = measurePositionAtSampleZero + quarterNoteLength;
            float remainder = (positionOfFirstQuarterNotePlusRemainder) % quarterNoteLength;
            float positionOfFirstQuarterNote = positionOfFirstQuarterNotePlusRemainder - remainder;

            secondLineBeatsFromSampleZero = Timing.GetBeatsBetweenMeasurePositions(
                timing, 
                measurePositionAtSampleZero, 
                positionOfFirstQuarterNote);
            offsetOfSecondLine = timing.MeasurePositionToOffset(positionOfFirstQuarterNote);
            bpmOfFirstLine = secondLineBeatsFromSampleZero / offsetOfSecondLine * 60f;

            ticksOfSecondLine = (int)(ticksPerBeat * secondLineBeatsFromSampleZero);
            firstTimeSignatureLine = GetTimeSignatureLine(0, lastTimingPoint.TimeSignature);
            firstBpmLine = GetBpmLine(0, bpmOfFirstLine);
            string secondBpmLine = GetBpmLine(ticksOfSecondLine, lastTimingPoint.Bpm);

            syncTrackSectionInner = firstTimeSignatureLine + firstBpmLine + secondBpmLine;

            return string.Format(syncTrackSectionUnFormatted, syncTrackSectionInner);
        }

        secondLineBeatsFromSampleZero = Timing.GetBeatsBetweenMeasurePositions(
            timing, 
            measurePositionAtSampleZero, 
            timingPointCreatingSecondLine.MeasurePosition!.Value);
        bpmOfFirstLine = secondLineBeatsFromSampleZero / timingPointCreatingSecondLine.Offset * 60;
        ticksOfSecondLine = (int)(ticksPerBeat * secondLineBeatsFromSampleZero);

        firstTimeSignatureLine = GetTimeSignatureLine(0, timingPointCreatingSecondLine.TimeSignature);
        firstBpmLine = GetBpmLine(0, bpmOfFirstLine);

        foreach ( TimingPoint timingPoint in timing.TimingPoints)
        {
            if (timingPoint.Offset <= 0 
                || Timing.AreMeasurePositionsEqual(timingPoint.MeasurePosition, measurePositionAtSampleZero))
                continue;
        }

        return null;
    }

    private string GetFirstSyncTrackLines(Timing timing)
    {
        float measurePositionAtOffsetZero = timing.OffsetToMeasurePosition(0);

        float beatLength = timing.GetDistancePerBeat(measurePositionAtOffsetZero);
        float positionOfNextQuarterNote = timing.GetOperatingBeatPosition(timing.OffsetToMeasurePosition(0)) + beatLength;

        float nextQuarterNoteBeatsFromOffsetZero = Timing.GetBeatsBetweenMeasurePositions(
            timing,
            measurePositionAtOffsetZero,
            positionOfNextQuarterNote);

        float offsetOfSecondLine = timing.MeasurePositionToOffset(positionOfNextQuarterNote);
        float bpmOfFirstLine = nextQuarterNoteBeatsFromOffsetZero / offsetOfSecondLine * 60f;

        string firstTimeSignatureLine = GetTimeSignatureLine(0, [1, 4]);
        string firstBpmLine = GetBpmLine(0, bpmOfFirstLine);

        int ticksOfSecondLine = (int)(ticksPerBeat * nextQuarterNoteBeatsFromOffsetZero);
        TimingPoint? operatingTimingPointOfNextQuarterNote = timing.GetOperatingTimingPoint_ByMeasurePosition(positionOfNextQuarterNote);
        string secondBpmLine = GetBpmLine(ticksOfSecondLine, operatingTimingPointOfNextQuarterNote!.Bpm);

        // We want a time signature of the second line such that the numerator in the time signature is the number of beats until the next measure
        // If the second line is already on a downbeat, 

        string secondTimeSignatureLine = GetTimeSignatureLine(ticksOfSecondLine, [4, 4]);

        return "";
    }

    private int FormatBpm(float bpm) => (int)Math.Round(bpm * 1000);

    private string FormatTimeSignature(int[] timeSignature)
    {
        Timing.CorrectTimeSignature(timeSignature, out timeSignature);
        int numerator = timeSignature[0];
        int denominator = timeSignature[1];
        int denominatorFormatted = (int)Math.Log2(denominator);

        return $"{numerator} {denominatorFormatted}";
    }

    private string GetBpmLine(int ticks, float bpm) => $"  {ticks} = B {FormatBpm(bpm)}\n";

    private string GetTimeSignatureLine(int ticks, int[] timeSignature)
    {
        if (timeSignature.Length != 2)
            throw new Exception("Incorrect time signature array length.");

        return $"  {ticks} = TS {FormatTimeSignature(timeSignature)}\n";
    }
}
