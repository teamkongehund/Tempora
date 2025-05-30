using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Tempora.Classes.Audio;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Utility;

public partial class BeatSaberExporter : Node
{
	public static BeatSaberExporter Instance = null!;

	public override void _Ready()
	{
		Instance = this;
	}

    private readonly JsonSerializerOptions serializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    
    // Assuming most users start with Ex+ diffs.
    private const string DefaultCharacteristic = "Standard";
    private const string DefaultDifficulty = "ExpertPlus";
    private const string DefaultDifficultyFilename = "ExpertPlusStandard.dat";

	public void SaveFilesToPath(string path)
    {
		AudioFile audioFile = Project.Instance.AudioFile;
        string audioFilename = Path.GetFileNameWithoutExtension(audioFile.FilePath);
		int audioSamples = audioFile.PcmLeft.Length - 1;

        Timing timing = Timing.CloneAndParseForBeatSaber(Timing.Instance);
        AddAdditionalTimingPointsIfRequired(timing, audioFile.AudacityOrigin);

        List<BpmDataPoint> bpmData = GetBpmDataPoints(timing.TimingPoints, audioFile);
        
        var zipPacker = new ZipPacker();
        string zipPath = Path.ChangeExtension(path, ".zip");
        zipPacker.Open(zipPath);

        bool isV4Format = Settings.Instance.BeatSaberExportFormat == 4;
        
        // Info file
        var info = new Info { SongName = audioFilename };
        string infoJson = JsonSerializer.Serialize(isV4Format ? info.GetV4Object() : info.GetV2Object(),
            serializeOptions);
        
        zipPacker.StartFile("Info.dat");
        zipPacker.WriteFile(infoJson.ToUtf8Buffer());
        zipPacker.CloseFile();
        
        // Audio data file
        var audioData = new AudioData
        {
            SongSampleCount = audioSamples,
            SongFrequency = audioFile.SampleRate,
            BpmData = bpmData
        };
        string audioDataJson = JsonSerializer.Serialize(isV4Format ? audioData.GetV4Object() : audioData.GetV2Object(),
            serializeOptions);
        string audioDataFilename = isV4Format ? "AudioData.dat" : "BPMInfo.dat";
        
        zipPacker.StartFile(audioDataFilename);
        zipPacker.WriteFile(audioDataJson.ToUtf8Buffer());
        zipPacker.CloseFile();
        
        // Difficulty files
        object difficultyObject = isV4Format ? Difficulty.GetV4Object() : Difficulty.GetV3Object(audioData.BpmData);
        string difficultyJson = JsonSerializer.Serialize(difficultyObject, serializeOptions);
        
        zipPacker.StartFile(DefaultDifficultyFilename);
        zipPacker.WriteFile(difficultyJson.ToUtf8Buffer());
        zipPacker.CloseFile();
        
        if (isV4Format)
        {
            zipPacker.StartFile("Lightshow.dat");
            zipPacker.WriteFile(difficultyJson.ToUtf8Buffer());
            zipPacker.CloseFile();
        }
        
        // Audio file
        if (audioFile.Extension == ".ogg")
        {
            zipPacker.StartFile("song.ogg");
            zipPacker.WriteFile(audioFile.FileBuffer);
            zipPacker.CloseFile();
        }
        else
        {
            Project.Instance.NotificationMessage = "Note: convert audio file to .ogg in Audacity";
        }
        
        zipPacker.Close();
	}

    private List<BpmDataPoint> GetBpmDataPoints(List<TimingPoint> timingPoints, AudioFile audioFile)
    {
        var bpmDataPoints = new List<BpmDataPoint>();
        
        int audioSamples = audioFile.PcmLeft.Length - 1;
        
        float currentBeat = 0f;
        int currentSample = 0;

        for (int i = 0; i < timingPoints.Count - 1; i++)
        {
            TimingPoint timingPoint = timingPoints[i];
            TimingPoint nextTimingPoint = timingPoints[i + 1];

            // Beat Saber doesn't work with measures. Convert to "beats"
            float measurePositionDiff = nextTimingPoint.MeasurePosition!.Value - timingPoint.MeasurePosition!.Value;
            float beatsDiff = measurePositionDiff * timingPoint.TimeSignature[0];

            float startBeat = currentBeat;
            float endBeat = startBeat + beatsDiff;

            // Beat Saber only supports ogg. If the user loads an mp3, odds are they're going to convert the audio file
            // to ogg via Audacity afterwards so offset the timings points.
            int startIndex = (int)((timingPoint.Offset + audioFile.AudacityOrigin) * audioFile.SampleRate);
            int endIndex = (int)((nextTimingPoint.Offset + audioFile.AudacityOrigin) * audioFile.SampleRate);
			
            var bpmDataPoint = new BpmDataPoint
            {
                StartIndex = startIndex,
                EndIndex = endIndex,
                StartBeat = startBeat,
                EndBeat = endBeat,
                Bpm = timingPoint.Bpm * (timingPoint.TimeSignature[1] / 4f)
            };
			
            currentBeat = endBeat;
            currentSample = endIndex;
			
            bpmDataPoints.Add(bpmDataPoint);
        }

        // Final region from last timing point to end of song
        TimingPoint lastTimingPoint = timingPoints.Last(); 
        float indexDiff = (float)audioSamples - currentSample;
        float secondsDiff = indexDiff / audioFile.SampleRate;
        
        bpmDataPoints.Add(new BpmDataPoint
        {
            StartIndex = currentSample,
            EndIndex = audioSamples,
            StartBeat = currentBeat,
            EndBeat = currentBeat + secondsDiff * (lastTimingPoint.Bpm / 60f),
            Bpm = lastTimingPoint.Bpm * (lastTimingPoint.TimeSignature[1] / 4f)
        });

        return bpmDataPoints;
    }

    // Beat Saber does not support offset and so always considers beat 0 as the start of the audio
    //  This method ensures we always have a timing point at 0 and an additional point to account for offset if present
    private static void AddAdditionalTimingPointsIfRequired(Timing timing, float timeOffset)
    {
        TimingPoint? firstTimingPoint = timing.TimingPoints.FirstOrDefault();
        if (firstTimingPoint?.MeasurePosition != null)
        {
            if (firstTimingPoint.Offset == 0) return;

            if (firstTimingPoint.MeasurePosition == 0)
            {
                // Add timing point to simulate a "beat" before the first timing point
                float secondsInFirstBeat = firstTimingPoint.Offset + timeOffset;
                float bpm = 60f / secondsInFirstBeat;
                    
                var offsetTimingPoint = new TimingPoint(-timeOffset, -0.25f, [4, 4]) { Bpm = bpm };
                timing.TimingPoints.Insert(0, offsetTimingPoint);
            }
            else
            {
                // Add a timing point at beat 0 
                float beats = Timing.GetBeatsBetweenMeasurePositions(timing, 0, (float)firstTimingPoint.MeasurePosition);
                float secondsPerBeat = (firstTimingPoint.Offset + timeOffset) / beats;
                float bpm = 60f / secondsPerBeat;

                var zeroTimingPoint = new TimingPoint(-timeOffset, 0, [4, 4]) { Bpm = bpm };
                timing.TimingPoints.Insert(0, zeroTimingPoint);
            }
        }
        else
        {
            // Audio is miraculously already aligned at default 120 bpm
            var initialTimingPoint = new TimingPoint(-timeOffset, 0, [4, 4]) { Bpm = 120 };
            timing.TimingPoints.Insert(0, initialTimingPoint);
        }
    }
    
    #region Data
    private struct BpmDataPoint
    {
        public required int StartIndex { get; set; }
        public required int EndIndex { get; set; }
        public required float StartBeat { get; set; }
        public required float EndBeat { get; set; }
        public required float Bpm { get; set; }
    }

    private class Info
    {
        public required string SongName { get; set; }

        public object GetV2Object()
        {
            return new
            {
                _version = "2.0.0",
                _songName = SongName,
                _songSubName = "",
                _songAuthorName = "",
                _levelAuthorName = "",
                _beatsPerMinute = 120,
                _songTimeOffset = 0,
                _shuffle = 0,
                _shufflePeriod = 0,
                _previewStartTime = 12,
                _previewDuration = 0,
                _songFilename = "song.ogg",
                _coverImageFilename = "cover.jpg",
                _environmentName = "DefaultEnvironment",
                _allDirectionsEnvironmentName = "GlassDessertEnvironment",
                _environmentNames = new List<string> { "DefaultEnvironment" },
                _colorSchemes = new List<object>(),
                _difficultyBeatmapSets = new List<object>
                {
                    new
                    {
                        _beatmapCharacteristicName = DefaultCharacteristic,
                        _difficultyBeatmaps = new List<object>
                        {
                            new
                            {
                                _difficulty = DefaultDifficulty,
                                _difficultyRank = 9,
                                _beatmapFilename = DefaultDifficultyFilename,
                                _noteJumpMovementSpeed = 0,
                                _noteJumpStartBeatOffset = 0,
                                _beatmapColorSchemeIdx = 0,
                                _environmentNameIdx = 0,
                            }
                        }
                    }
                }
            };
        }
        
        public object GetV4Object()
        {
            return new
            {
                Version = "4.0.0",
                Song = new
                {
                    Title = SongName,
                    SubTitle = "",
                    Author = ""
                },
                Audio = new
                {
                    SongFilename = "song.ogg",
                    SongDuration = 0, // Leaving this as default as it's calculated on save in every editor
                    AudioDataFilename = "AudioData.dat",
                    Bpm = 120,
                    Lufs = 0,
                    PreviewStartTime = 12,
                    PreviewDuration = 10
                },
                SongPreviewFilename = "song.ogg",
                CoverImageFilename = "cover.jpg",
                EnvironmentNames = new List<string> { "DefaultEnvironment" },
                ColorSchemes = (object[])[],
                DifficultyBeatmaps = new List<object>
                {
                    new
                    {
                        Characteristic = DefaultCharacteristic,
                        Difficulty = DefaultDifficulty,
                        BeatmapAuthors = new
                        {
                            Mappers = new List<string>(),
                            Lighters = new List<string>()
                        },
                        EnvironmentNameIdx = 0,
                        BeatmapColorSchemeIdx = 0,
                        NoteJumpMovementSpeed = 0,
                        NoteJumpStartBeatOffset = 0,
                        BeatmapDataFilename = DefaultDifficultyFilename,
                        LightshowDataFilename = "Lightshow.dat"
                    }
                }
            };
        }
    }

	private class AudioData
	{
		public required int SongSampleCount { get; init; }
		public required int SongFrequency { get; init; }
		public required List<BpmDataPoint> BpmData { get; init; } = [];

        public object GetV4Object()
        {
            return new
            {
                Version = "4.0.0",
                SongCheckSum = "",
                SongSampleCount = SongSampleCount,
                SongFrequency = SongFrequency,
                BpmData = BpmData.Select(x => new
                {
                    si = x.StartIndex,
                    ei = x.EndIndex,
                    sb = x.StartBeat,
                    eb = x.EndBeat
                }),
            };
        }
        
        public object GetV2Object()
        {
            return new
            {
                _version = "2.0.0",
                _songSampleCount = SongSampleCount,
                _songFrequency = SongFrequency,
                _regions = BpmData.Select(x => new
                {
                    _startSampleIndex = x.StartIndex,
                    _endSampleIndex = x.EndIndex,
                    _startBeat = x.StartBeat,
                    _endBeat = x.EndBeat
                })
            };
        }
	}

    private static class Difficulty
    {
        public static object GetV4Object()
        {
            // Not having to specify default properties in v4 is nice
            return new { Version = "4.0.0" };
        }
        
        public static object GetV3Object(IEnumerable<BpmDataPoint> bpmDataPoints)
        {
            // Some editors do not read from BPMInfo.dat so we need to populate the BpmEvents in the difficulty file
            var bpmEvents = bpmDataPoints.Select(x => new { b = x.StartBeat, m = x.Bpm });
            
            return new
            {
                Version = "3.3.0",
                BpmEvents = bpmEvents,
                RotationEvents = new List<object>(),
                ColorNotes = new List<object>(),
                BombNotes = new List<object>(),
                ObstaclesNotes = new List<object>(),
                Sliders = new List<object>(),
                BurstSliders = new List<object>(),
                BasicBeatmapEvents = new List<object>(),
                ColorBoostBeatmapEvents = new List<object>(),
                Waypoints = new List<object>(),
                BasicEventTypesWithKeywords = new List<object>
                {
                    new
                    {
                        d = new List<object>()
                    }
                },
                LightColorEventBoxGroups = new List<object>(),
                LightRotationEventBoxGroups = new List<object>(),
                LightTranslationEventBoxGroups = new List<object>(),
                VfxEventBoxGroups = new List<object>(),
                _fxEventsCollections = new
                {
                    _fl = new List<object>(),
                    _il = new List<object>(),
                },
                UseNormalEventsAsCompatibleEvents = false
            };
        }
    }
    #endregion
}
