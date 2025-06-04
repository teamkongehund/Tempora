using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;

namespace Tempora.Classes.DataHelpers;
public class GuitarGameExporter
{
    private static int ticksPerBeat = 192;

    private static string syncTrackSectionUnformatted = @"[SyncTrack]
{{{0}
}}";

    private static string songSection = @"[Song]
{
  Name = ""Tempora Export""
  Offset = 0
  Resolution = 192
  MusicStream = ""song.ogg""
}" + "\n";

    public static void SaveChartToPath_AndShowInFileExplorer(Timing timing, string path)
    {
        path = Path.ChangeExtension(path, "chart");
        SaveChartToPath(timing, path);
        OS.ShellShowInFileManager(path);
    }

    public static void SaveChartToPath(Timing timing, string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException("path");

        path = Path.ChangeExtension(path, "chart");
        string syncTrackSection = GetSyncTrackSectionNew(timing);
        string notesChartText = songSection + syncTrackSection;

        FileHandler.SaveText(path, notesChartText);
    }

    private static string GetSyncTrackSectionNew(Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);

        if (timing.TimingPoints.Count == 0)
            return string.Format(syncTrackSectionUnformatted, GetTimeSignatureLine(0, [4,4]) + GetBpmLine(0, 120));

        string syncTrackSectionInner = "";
        syncTrackSectionInner += GetFirstSyncTrackLines(timing, out float positionOfReferenceBeat, out int positionOfReferenceDownbeat, out int ticksOfReferenceDownbeat);

        int getTicks(float measurePosition) => GetTicks(measurePosition, timing, ticksOfReferenceDownbeat, positionOfReferenceDownbeat);

        float epsilon = 0.001f;

        foreach (TimeSignaturePoint point in timing.TimeSignaturePoints)
        {
            if (point.Measure <= positionOfReferenceDownbeat)
                continue;

            syncTrackSectionInner += GetTimeSignatureLine(getTicks(point.Measure), point.TimeSignature);
        }

        foreach (TimingPoint point in timing.TimingPoints)
        {
            if (point.MeasurePosition!.Value - positionOfReferenceBeat < epsilon)
                continue;

            syncTrackSectionInner += GetBpmLine(getTicks(point!.MeasurePosition.Value), point.Bpm);
        }

        return string.Format(syncTrackSectionUnformatted, syncTrackSectionInner);
    }

    private static string GetFirstSyncTrackLines(Timing timing, out float positionOfReferenceBeat, out int positionOfReferenceDownbeat, out int ticksOfReferenceDownbeat)
    {
        float measurePositionAtOffsetZero = timing.OffsetToMeasurePosition(0);

        float beatLength = timing.GetDistancePerBeat(measurePositionAtOffsetZero);
        positionOfReferenceBeat = timing.GetOperatingBeatPosition(timing.OffsetToMeasurePosition(0)) + beatLength;

        //float referenceBeatBeatsFromOffsetZero = Timing.GetBeatsBetweenMeasurePositions(
        //    timing,
        //    measurePositionAtOffsetZero,
        //    positionOfReferenceBeat);

        float offsetOfSecondLine = timing.MeasurePositionToOffset(positionOfReferenceBeat);
        float bpmOfFirstLine = 1 / offsetOfSecondLine * 60f;

        string firstTimeSignatureLine = GetTimeSignatureLine(0, [1, 4]);
        string firstBpmLine = GetBpmLine(0, bpmOfFirstLine);

        int ticksOfSecondLine = (int)(ticksPerBeat * 1);
        TimingPoint? operatingTimingPointOfReferenceBeat = timing.GetOperatingTimingPoint_ByMeasurePosition(positionOfReferenceBeat);
        string secondBpmLine = GetBpmLine(ticksOfSecondLine, operatingTimingPointOfReferenceBeat!.Bpm);

        bool isReferenceBeatOnDownbeat = Timing.AreMeasurePositionsEqual(positionOfReferenceBeat, (int)positionOfReferenceBeat)
            || Timing.AreMeasurePositionsEqual(positionOfReferenceBeat, (int)positionOfReferenceBeat + 1);

        float epsilon = 0.001f;
        positionOfReferenceDownbeat = isReferenceBeatOnDownbeat ? (int)(positionOfReferenceBeat + epsilon) : (int)(positionOfReferenceBeat + 1 + epsilon);
        int beatsFromRerefenceBeatToReferenceDownbeat = isReferenceBeatOnDownbeat 
            ? 0 
            : (int)Timing.GetBeatsBetweenMeasurePositions(timing, positionOfReferenceBeat, positionOfReferenceDownbeat);

        string secondTimeSignatureLine = isReferenceBeatOnDownbeat ? "" : GetTimeSignatureLine(ticksOfSecondLine, [beatsFromRerefenceBeatToReferenceDownbeat, 4]);

        ticksOfReferenceDownbeat = (int)(ticksOfSecondLine + beatsFromRerefenceBeatToReferenceDownbeat * ticksPerBeat);
        string thirdTimeSignatureLine = GetTimeSignatureLine(ticksOfReferenceDownbeat, timing.GetTimeSignature(positionOfReferenceDownbeat));

        return firstTimeSignatureLine + firstBpmLine + secondTimeSignatureLine + secondBpmLine + thirdTimeSignatureLine;
    }

    private static int GetTicks(float measurePosition, Timing timing, int ticksOfReferenceDownbeat, int positionOfReferenceDownbeat) 
        => (int)(ticksOfReferenceDownbeat + Timing.GetBeatsBetweenMeasurePositions(timing, positionOfReferenceDownbeat, measurePosition) * ticksPerBeat);

    private static int FormatBpm(float bpm) => (int)Math.Round(bpm * 1000);

    private static string FormatTimeSignature(int[] timeSignature)
    {
        Timing.CorrectTimeSignature(timeSignature, out timeSignature);
        int denominatorFormatted = (int)Math.Log2(timeSignature[1]);

        return $"{timeSignature[0]} {denominatorFormatted}";
    }

    private static string GetBpmLine(int ticks, float bpm) => $"\n  {ticks} = B {FormatBpm(bpm)}";

    private static string GetTimeSignatureLine(int ticks, int[] timeSignature)
    {
        if (timeSignature.Length != 2)
            throw new Exception("Incorrect time signature array length.");

        return $"\n  {ticks} = TS {FormatTimeSignature(timeSignature)}";
    }
}
