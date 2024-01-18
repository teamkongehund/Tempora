using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using OsuTimer.Classes.Audio;

namespace OsuTimer.Classes.Utility;

public partial class OsuExporter : Node
{
    public static int ExportOffsetMs = -29;

    public static string DefaultDotOsuFormer = @"osu file format v14

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
Tags: tempora
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

    public static string DefaultDotOsuLatter = @"

[HitObjects]";

    public static string GetDotOsu(Timing timing)
    {
        var newTiming = Timing.CopyAndAddExtraPoints(timing);
        var timingPoints = newTiming.TimingPoints;
        string timingPointsData = TimingPointToText(timingPoints);
        var dotOsu = $"{DefaultDotOsuFormer}{timingPointsData}{DefaultDotOsuLatter}";
        return dotOsu;
    }

    public static string TimingPointToText(List<TimingPoint> timingPoints)
    {
        var theText = "";
        foreach (var timingPoint in timingPoints)
        {
            //GD.Print(timingPoint.Bpm);
            theText += TimingPointToText(timingPoint);
        }

        return theText;
    }

    public static string TimingPointToText(TimingPoint timingPoint)
    {
        // offsetMS,MSPerBeat,beatsInMeasure,sampleSet,sampleIndex,volume,uninherited,effects
        var offsetMs = ((int)(timingPoint.Time * 1000) + ExportOffsetMs).ToString();
        var msPerBeat = (timingPoint.BeatLengthSec * 1000).ToString(CultureInfo.InvariantCulture);
        var beatsInMeasure = timingPoint.TimeSignature[0].ToString();
        return $"{offsetMs},{msPerBeat},{beatsInMeasure},2,0,80,1,0\n";
    }

    public static void SaveOsz(string oszPath, string dotOsuString, AudioFile audioFile)
    {
        using (var zipPacker = new ZipPacker())
        {
            var err = zipPacker.Open(oszPath);
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
    }

    public static void SaveOsu(string osuPath, string dotOsuString)
    {
        FileHandler.SaveText(osuPath, dotOsuString);
    }

    public static void ExportOsz()
    {
        var random = new Random();
        int rand = random.Next();
        var path = $"user://{rand}.osz";
        string dotOsu = GetDotOsu(Timing.Instance);
        SaveOsz(path, dotOsu, Project.Instance.AudioFile);

        // Open with system:
        string globalPath = ProjectSettings.GlobalizePath(path);
        if (FileAccess.FileExists(globalPath)) OS.ShellOpen(globalPath);
    }
}