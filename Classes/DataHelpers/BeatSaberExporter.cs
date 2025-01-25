using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Tempora.Classes.TimingClasses;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tempora.Classes.Utility;

public partial class BeatSaberExporter : Node
{
	public static BeatSaberExporter Instance = null!;

	public override void _Ready()
	{
		Instance = this;
	}

	public void SaveFile()
	{
		var timing = Timing.CopyTiming(Timing.Instance);

		var audioFile = Project.Instance.AudioFile;
		var audioSamples = Math.Round(audioFile.SampleRate * audioFile.GetAudioLength());
		var bpmData = new List<BpmDataPoint>();

        var defaultInitialTimingPoint = new TimingPoint(-audioFile.AudacityOrigin, 0, [4, 4]) { Bpm = 120 };
        
        var firstTimingPoint = timing.TimingPoints.FirstOrDefault();
        if (firstTimingPoint != null)
        {
            if (firstTimingPoint.Offset < 0)
            {
                // Honestly not sure how to handle this case
                timing.TimingPoints.RemoveAt(0);
                timing.TimingPoints.Insert(0, defaultInitialTimingPoint);
            }
            else if (firstTimingPoint.Offset > 0)
            {
			    timing.TimingPoints.Insert(0, defaultInitialTimingPoint);
            }
        }
        else
        {
            // Wow the audio is miraculously already aligned at 120 bpm
            timing.TimingPoints.Insert(0, defaultInitialTimingPoint);
        }


        var currentBeat = 0f;
		var currentSample = 0;

		for (var i = 0; i < timing.TimingPoints.Count - 1; i++)
		{
			var timingPoint = timing.TimingPoints[i];
			var nextTimingPoint = timing.TimingPoints[i + 1];

            // Beat Saber doesn't work with measures. Convert to "beats"
			var musicPositionDiff = nextTimingPoint.MusicPosition!.Value - timingPoint.MusicPosition!.Value;
			// var beatInMeasure =  / (timingPoint.TimeSignature[1] / 4f);
			var beatsDiff = musicPositionDiff * timingPoint.TimeSignature[0];

			var startBeat = currentBeat;
			var endBeat = startBeat + beatsDiff;

            // Converting mp3 to ogg in Audacity afterwards will screw with offset so do this
			var startIndex = (int)((timingPoint.Offset + audioFile.AudacityOrigin) * audioFile.SampleRate);
			var endIndex = (int)((nextTimingPoint.Offset + audioFile.AudacityOrigin) * audioFile.SampleRate);
			
			var bpmDataPoint = new BpmDataPoint
			{
				StartIndex = startIndex,
				EndIndex = endIndex,
				StartBeat = startBeat,
				EndBeat = endBeat,
			};
			
			currentBeat = endBeat;
			currentSample = endIndex;
			
			bpmData.Add(bpmDataPoint);
		}

		// Final timing point
        GD.Print(string.Join(", ", timing.TimingPoints.Select(x => x.Bpm)));
		var finalBpm = timing.TimingPoints.Last().Bpm;
		var indexDiff = (float)audioSamples - currentSample;
		var secondsDiff = indexDiff / audioFile.SampleRate;
        
		bpmData.Add(new ()
		{
			StartIndex = currentSample,
			EndIndex = (int)audioSamples,
			StartBeat = currentBeat,
			EndBeat = currentBeat + secondsDiff * (finalBpm / 60f),
		});
		
		var output = new AudioData
		{
			SongSampleCount = (int)audioSamples,
			SongFrequency = audioFile.SampleRate,
			BpmData = bpmData,
			LufsData = []
		};
		
		var serializeOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};
		
		var jsonString = JsonSerializer.Serialize(output, serializeOptions);

        
        var zipPacker = new ZipPacker();
        var audioDirectory = Path.GetDirectoryName(audioFile.FilePath);

        if (audioDirectory != null)
        {
            var zipPath = Path.Combine(audioDirectory, "BeatSaberMapTemplate.zip");
            zipPacker.Open(zipPath);
            
            zipPacker.StartFile("AudioData.dat");
            zipPacker.WriteFile(jsonString.ToUtf8Buffer());
            zipPacker.CloseFile();
            
            zipPacker.Close();
        }
	}

	private class AudioData
	{
		public string Version { get; set; } = "4.0.0";
		public string SongCheckSum { get; set; } = ""; // Unused property in customs
		public int SongSampleCount { get; set; }
		public int SongFrequency { get; set; }

		public List<BpmDataPoint> BpmData { get; set; } = [];
		public List<object> LufsData { get; set; } = [];
	}
	
	private class BpmDataPoint
	{
		[JsonPropertyName("si")]
		public int StartIndex { get; set; }
		
		[JsonPropertyName("ei")]
		public int EndIndex { get; set; }
		
		[JsonPropertyName("sb")]
		public float StartBeat { get; set; }
		
		[JsonPropertyName("eb")]
		public float EndBeat { get; set; }
	}
}
