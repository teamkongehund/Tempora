using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class OsuExporter : Node
{
	public static int ExportOffsetMS = -29;

	public static string GetDotOsu(Timing timing)
	{
        List<TimingPoint> timingPoints = timing.TimingPoints;
		string timingPointsData = TimingPointToText(timingPoints);
		string dotOsu = $"{DefaultDotOsuFormer}{timingPointsData}{DefaultDotOsuLatter}";
		//GD.Print(dotOsu);
		return dotOsu;
    }

	public static string TimingPointToText(List<TimingPoint> timingPoints)
	{
		string theText = "";
		foreach(TimingPoint timingPoint in timingPoints)
		{
			theText += TimingPointToText(timingPoint);
		}
		return theText;
	}

	public static string TimingPointToText(TimingPoint timingPoint)
	{
		// offsetMS,MSPerBeat,beatsInMeasure,sampleSet,sampleIndex,volume,uninherited,effects
		string offsetMS = ((int)(timingPoint.Time * 1000) + ExportOffsetMS).ToString();
		string MSPerBeat = (timingPoint.BeatLength*1000).ToString(CultureInfo.InvariantCulture);
		string beatsInMeasure = timingPoint.TimeSignature[0].ToString();
		return $"{offsetMS},{MSPerBeat},{beatsInMeasure},2,0,80,1,0\n";
    }

	public static void SaveOsz(string oszPath, string dotOsuString, AudioFile audioFile)
	{
		using (ZipPacker zipPacker = new ZipPacker())
		{
			var err = zipPacker.Open(oszPath);
			if (err != Error.Ok)
				return;

			zipPacker.StartFile("song.osu");
			zipPacker.WriteFile(dotOsuString.ToUtf8Buffer());
			zipPacker.CloseFile();

			zipPacker.StartFile("audio.mp3");
			zipPacker.WriteFile(FileHandler.GetFileAsBuffer(audioFile.Path));
			zipPacker.CloseFile();

			zipPacker.Close();
		}
    }

	public static void SaveOsu(string osuPath, string dotOsuString)
	{
		FileHandler.SaveText(osuPath, dotOsuString);
	}

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
Tags: osutimer
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
}