using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Utility;

public partial class OsuExporter : Node
{
    private static int exportOffsetMs = -29;

    private static string defaultDotOsuFormer = @"osu file format v14

[General]
AudioFilename: audio.mp3
AudioLeadIn: 0
PreviewTime: -1
Countdown: 1
SampleSet: Soft
StackLeniency: 0.7
Mode: 0
LetterboxInBreaks: 0
WidescreenStoryboard: 0

[Editor]
DistanceSpacing: 1.3
BeatDivisor: 4
GridSize: 32
TimelineZoom: 1

[Metadata]
Title:
TitleUnicode:
Artist:
ArtistUnicode:
Creator:
Version:
Source:
Tags: tempora_timer
BeatmapID:0
BeatmapSetID:-1

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:8
ApproachRate:9
SliderMultiplier:1.4
SliderTickRate:1

[Events]
//Background and Video events
//Break Periods
//Storyboard Layer 0 (Background)
//Storyboard Layer 1 (Fail)
//Storyboard Layer 2 (Pass)
//Storyboard Layer 3 (Foreground)
//Storyboard Layer 4 (Overlay)
//Storyboard Sound Samples

[TimingPoints]
";

    private static string defaultDotOsuLatter = @"

[HitObjects]";

    public static int ExportOffsetMs { get => exportOffsetMs; set => exportOffsetMs = value; }
    public static string DefaultDotOsuFormer { get => defaultDotOsuFormer; set => defaultDotOsuFormer = value; }
    public static string DefaultDotOsuLatter { get => defaultDotOsuLatter; set => defaultDotOsuLatter = value; }

    public static string GetDotOsu(Timing timing)
    {
        var newTiming = Timing.CopyAndAddExtraPoints(timing);
        List<TimingPoint> timingPoints = newTiming.TimingPoints;
        string timingPointsData = TimingPointToText(timingPoints);
        string dotOsu = $"{DefaultDotOsuFormer}{timingPointsData}{DefaultDotOsuLatter}";
        return dotOsu;
    }

    public static string TimingPointToText(List<TimingPoint> timingPoints)
    {
        string theText = "";
        foreach (TimingPoint timingPoint in timingPoints)
        {
            //GD.Print(timingPoint.Bpm);
            theText += TimingPointToText(timingPoint);
        }

        return theText;
    }

    public static string TimingPointToText(TimingPoint timingPoint)
    {
        // offsetMS,MSPerBeat,beatsInMeasure,sampleSet,sampleIndex,volume,uninherited,effects
        string offsetMs = ((int)(timingPoint.Offset * 1000) + ExportOffsetMs).ToString();
        string msPerBeat = (timingPoint.BeatLengthSec * 1000).ToString(CultureInfo.InvariantCulture);
        string beatsInMeasure = timingPoint.TimeSignature[0].ToString();
        return $"{offsetMs},{msPerBeat},{beatsInMeasure},2,0,80,1,0\n";
    }

    public static void SaveOsz(string oszPath)
    {
        Path.ChangeExtension(oszPath, "osz");

        string dotOsu = GetDotOsu(Timing.Instance);

        SaveOsz(oszPath, dotOsu, Project.Instance.AudioFile);
    }

    public static void SaveOsz(string oszPath, string dotOsuString, AudioFile audioFile)
    {
        using var zipPacker = new ZipPacker();
        Error err = zipPacker.Open(oszPath);
        if (err != Error.Ok)
            return;

        var random = new Random();
        int rand = random.Next();

        zipPacker.StartFile($"{rand}.osu");
        zipPacker.WriteFile(dotOsuString.ToUtf8Buffer());
        zipPacker.CloseFile();

        zipPacker.StartFile("audio.mp3");
        //zipPacker.WriteFile(FileHandler.GetFileAsBuffer(audioFile.Path));
        zipPacker.WriteFile(((AudioStreamMP3)audioFile.Stream).Data);
        zipPacker.CloseFile();

        zipPacker.Close();
    }

    public static void SaveOsu(string osuPath, string dotOsuString) => FileHandler.SaveText(osuPath, dotOsuString);

    public static void ExportAndOpenOsz()
    {
        var random = new Random();
        int rand = random.Next();
        string path = $"user://{rand}.osz";
        string dotOsu = GetDotOsu(Timing.Instance);
        SaveOsz(path, dotOsu, Project.Instance.AudioFile);

        // Open with system:
        string globalPath = ProjectSettings.GlobalizePath(path);
        if (Godot.FileAccess.FileExists(globalPath))
            OS.ShellOpen(globalPath);
    }

    public static void ExportOszAsAndOpenDirectory(string path)
    {
        SaveOsz(path);

        // Open directory with system:
        string globalPath = ProjectSettings.GlobalizePath(path);
        globalPath = FileHandler.GetDirectory(globalPath);
        if (Godot.FileAccess.FileExists(globalPath))
            OS.ShellOpen(globalPath);
    }
}