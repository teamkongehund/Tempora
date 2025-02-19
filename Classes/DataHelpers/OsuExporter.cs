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
    private static string defaultDotOsuFormer = @"osu file format v14

[General]
AudioFilename: audio{0}
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
Tags:
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

    private int exportOffsetMs => Settings.Instance.ExportOffsetMs;
    public static string DefaultDotOsuFormer { get => defaultDotOsuFormer; set => defaultDotOsuFormer = value; }
    public static string DefaultDotOsuLatter { get => defaultDotOsuLatter; set => defaultDotOsuLatter = value; }


    public static OsuExporter Instance = null!;

    public override void _Ready()
    {
        Instance = this;
    }

    public string GetDotOsu(Timing timing, AudioFile audioFile)
    {
        var newTiming = Timing.CloneAndParseForOsu(timing, audioFile);
        if (Settings.Instance.PreventDoubleBarlines)
            FixBpmsToEnsureProperLineups(newTiming);
        string timingPointsData = TimingToDotOsuTimingPoints(newTiming);
        string extension = audioFile.Extension;
        string dotOsuUnformatted = $"{DefaultDotOsuFormer}{timingPointsData}{DefaultDotOsuLatter}";
        string dotOsu = String.Format(dotOsuUnformatted, extension);
        return dotOsu;
    }

    public string TimingToDotOsuTimingPoints(Timing timing)
    {
        if (timing.TimingPoints == null)
            throw new NullReferenceException("timing.TimingPoints was null");

        string result = "";

        for (int i = 0; i < timing.TimingPoints!.Count; i++)
        {
            var timingPoint = timing.TimingPoints[i];
            TimingPoint? previousTimingPoint = i > 0 ? timing.TimingPoints?[i - 1] : null;
            result += TimingPointToDotOsuLine(timingPoint);
        }

        return result;
    }

    private static bool ShouldOmitBarline(TimingPoint timingPoint) => Settings.Instance.OmitBarlines ? timingPoint.MeasurePosition % 1 != 0 : false;

    private string TimingPointToDotOsuLine(TimingPoint timingPoint)
    {
        string offsetMs = ((int)(timingPoint.Offset * 1000) + exportOffsetMs).ToString();
        string msPerBeat = (timingPoint.BeatLengthSec * 1000).ToString(CultureInfo.InvariantCulture);
        string beatsInMeasure = timingPoint.TimeSignature[0].ToString();
        bool omit = ShouldOmitBarline(timingPoint);
        string effects = omit ? "8" : "0";
        return $"{offsetMs},{msPerBeat},{beatsInMeasure},2,0,80,1,{effects}\n";
    }

    private static void FixBpmsToEnsureProperLineups(Timing timing)
    {
        timing.ShouldHandleTimingPointChanges = false;
        for (int i = 0; i < timing.TimingPoints!.Count; i++)
        {
            TimingPoint timingPoint = timing.TimingPoints[i];
            TimingPoint? previousTimingPoint = i >= 1 ? timing.TimingPoints[i - 1] : null;

            if (previousTimingPoint == null) 
                continue;

            float previousOffsetMsRounded = (int)(previousTimingPoint.Offset * 1000);
            float measureDifference = (float)(timingPoint.MeasurePosition! - previousTimingPoint.MeasurePosition!);
            float timeDifference = measureDifference / previousTimingPoint.MeasuresPerSecond;
            float previousWhiteLineOffsetMsRounded = (int)(previousOffsetMsRounded + timeDifference * 1000);
            float offsetMsRounded = (int)(timingPoint.Offset * 1000);
            if (offsetMsRounded <= previousWhiteLineOffsetMsRounded)
                continue;

            float requiredTimeDifference = timeDifference + (0.001f - timeDifference % 0.001f + 0.0001f);
            float requiredMeasuresPerSecond = measureDifference / requiredTimeDifference;
            previousTimingPoint.MeasuresPerSecond = requiredMeasuresPerSecond;
            previousTimingPoint.Bpm = previousTimingPoint.MpsToBpm(requiredMeasuresPerSecond);
        }
        timing.ShouldHandleTimingPointChanges = true;
    }

    public void SaveOsz(string oszPath, out string changedPath)
    {
        oszPath = Path.ChangeExtension(oszPath, ".osz");
        changedPath = oszPath;

        string dotOsu = GetDotOsu(Timing.Instance, Project.Instance.AudioFile);

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

        zipPacker.StartFile($"audio{audioFile.Extension}");
        
        // If osu export no longer works, uncomment this:
        //byte[] fileBuffer = (audioFile.Extension == ".mp3")
        //    ? ((AudioStreamMP3)audioFile.Stream).Data
        //    : FileHandler.GetFileAsBuffer(audioFile.filePath);

        byte[] fileBuffer = audioFile.FileBuffer;

        zipPacker.WriteFile(fileBuffer);

        zipPacker.CloseFile();

        zipPacker.Close();
    }

    public static void SaveOsu(string osuPath, string dotOsuString) => FileHandler.SaveText(osuPath, dotOsuString);

    public void ExportAndOpenOsz()
    {
        var random = new Random();
        int rand = random.Next();
        string path = $"user://{rand}.osz";
        string dotOsu = GetDotOsu(Timing.Instance, Project.Instance.AudioFile);
        SaveOsz(path, dotOsu, Project.Instance.AudioFile);

        string globalPath = ProjectSettings.GlobalizePath(path);
        if (Godot.FileAccess.FileExists(globalPath))
            OS.ShellOpen(globalPath);
    }

    public void SaveOszAs_AndShowInFileExplorer(string path)
    {
        SaveOsz(path, out path);
        OS.ShellShowInFileManager(path);
    }
}