using System;
using System.Globalization;
using System.IO;
using Godot;
using OsuTimer.Classes.Audio;

namespace OsuTimer.Classes.Utility;

/// <summary>
///     Handles saving and loading of project files
/// </summary>
public partial class ProjectFileManager : Node
{
    public static ProjectFileManager Instance = null!;

    public static readonly string ProjectFileExtension = "tmpr";
    private Settings settings = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
        settings = Settings.Instance;
    }

    public void SaveProjectAs(string filePath)
    {
        string extension = FileHandler.GetExtension(filePath);

        string correctExtension = ProjectFileManager.ProjectFileExtension;

        if (extension != correctExtension)
            filePath += "." + correctExtension;

        string filePathWithoutExtension = filePath;
        filePathWithoutExtension = Path.ChangeExtension(filePathWithoutExtension, null);

        string mp3Path = $"{filePathWithoutExtension}.mp3";

        string file = GetProjectAsString(mp3Path);
        FileHandler.SaveMP3(mp3Path, (AudioStreamMP3)Project.Instance.AudioFile.Stream);
        FileHandler.SaveText(filePath, file);
    }

    public string GetProjectAsString() => GetProjectAsString(Project.Instance.AudioFile.Path);

    public string GetProjectAsString(string audioPath)
    {
        // TimeSignaturePoint
        // MusicPosition;TimeSignatureUpper;TimeSignatureLower
        string timeSignaturePointsLines = "";
        foreach (TimeSignaturePoint timeSignaturePoint in Timing.Instance.TimeSignaturePoints)
        {
            string timeSignaturePointLine = "";
            timeSignaturePointLine += timeSignaturePoint.MusicPosition.ToString(CultureInfo.InvariantCulture);
            timeSignaturePointLine += ";";
            timeSignaturePointLine += timeSignaturePoint.TimeSignature[0].ToString();
            timeSignaturePointLine += ";";
            timeSignaturePointLine += timeSignaturePoint.TimeSignature[1].ToString();
            timeSignaturePointLine += "\n";

            timeSignaturePointsLines += timeSignaturePointLine;
        }

        string timingPointsLines = "";
        if ((Timing.Instance?.TimingPoints?.Count ?? 0) > 0)
            timingPointsLines = GetTimingPointsAsString();

        string file = "";
        file += "[AudioPath]\n";
        file += audioPath + "\n";
        file += "[TimeSignaturePoints]\n";
        file += timeSignaturePointsLines;
        file += "[TimingPoints]\n";
        file += timingPointsLines;

        return file;
    }

    private string GetTimingPointsAsString()
    {
        // Time;MusicPosition;TimeSignatureUpper;TimeSignatureLower
        string timingPointsLines = "";
        TimingPoint lastTimingPoint = Timing.Instance.TimingPoints[^1];
        foreach (TimingPoint timingPoint in Timing.Instance.TimingPoints)
        {
            if (timingPoint?.MusicPosition == null)
                continue;
            string timingPointLine = "";
            timingPointLine += timingPoint.Time.ToString(CultureInfo.InvariantCulture);
            timingPointLine += ";";
            timingPointLine += ((float)timingPoint.MusicPosition).ToString(CultureInfo.InvariantCulture);
            timingPointLine += ";";
            timingPointLine += timingPoint.TimeSignature[0].ToString();
            timingPointLine += ";";
            timingPointLine += timingPoint.TimeSignature[1].ToString();
            if (timingPoint == lastTimingPoint)
                timingPointLine += ";" + timingPoint.MeasuresPerSecond.ToString(CultureInfo.InvariantCulture);
            timingPointLine += "\n";

            timingPointsLines += timingPointLine;
        }
        return timingPointsLines;
    }

    private void LoadProjectFromFile(string projectFile)
    {
        Timing.Instance = new Timing
        {
            IsInstantiating = true
        };

        string[] lines = projectFile.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string audioPath = "";

        ParseMode parseMode = ParseMode.None;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line == "TimingPoints")
                parseMode = ParseMode.TimingPoints;

            switch (line)
            {
                case "[AudioPath]":
                    parseMode = ParseMode.AudioPath;
                    continue;
                case "[TimeSignaturePoints]":
                    parseMode = ParseMode.TimeSignaturePoints;
                    continue;
                case "[TimingPoints]":
                    parseMode = ParseMode.TimingPoints;
                    continue;
            }

            string[] lineData = line.Split(";");

            switch (parseMode)
            {
                case ParseMode.AudioPath:
                    audioPath = line;
                    continue;
                case ParseMode.TimeSignaturePoints:
                    if (lineData.Length != 3)
                        continue;

                    bool tsMusicPositionParsed = int.TryParse(lineData[0], out int tsMusicPosition);
                    bool timeSignatureUpperParsed = int.TryParse(lineData[1], out int timeSignatureUpper);
                    bool timeSignatureLowerParsed = int.TryParse(lineData[2], out int timeSignatureLower);
                    if (tsMusicPositionParsed == false
                        || timeSignatureUpperParsed == false
                        || timeSignatureLowerParsed == false
                       )
                    {
                        continue;
                    }

                    Timing.Instance.UpdateTimeSignature(new[] { timeSignatureUpper, timeSignatureLower }, tsMusicPosition);

                    break;
                case ParseMode.TimingPoints:
                    if (lineData.Length is not 4 and not 5)
                        continue;

                    bool timeParsed = float.TryParse(
                        lineData[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float time);
                    bool tpMusicPositionParsed = float.TryParse(
                        lineData[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float tpMusicPosition);
                    timeSignatureUpperParsed = int.TryParse(lineData[2], out _);
                    timeSignatureLowerParsed = int.TryParse(lineData[3], out _);

                    bool measuresPerSecondParsed = true;
                    float measuresPerSecond = 2f;
                    if (lineData.Length == 5)
                        measuresPerSecondParsed = float.TryParse(lineData[4], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out measuresPerSecond);

                    if (timeParsed == false
                        || tpMusicPositionParsed == false
                        || timeSignatureUpperParsed == false
                        || timeSignatureLowerParsed == false
                        || measuresPerSecondParsed == false
                       )
                    {
                        continue;
                    }

                    switch (lineData.Length)
                    {
                        case 4:
                            Timing.Instance.AddTimingPoint(tpMusicPosition, time);
                            break;
                        case 5:
                            Timing.Instance.AddTimingPoint(tpMusicPosition, time, measuresPerSecond);
                            break;
                    }

                    break;
            }
        }

        Project.Instance.AudioFile = new AudioFile(audioPath);
        Timing.Instance.IsInstantiating = false;
        _ = Signals.Instance.EmitSignal("TimingChanged");
    }

    public void LoadProjectFromFilePath(string filePath)
    {
        string projectFile = FileHandler.LoadText(filePath);
        if (string.IsNullOrEmpty(projectFile))
            return;
        LoadProjectFromFile(projectFile);
    }

    private enum ParseMode
    {
        None,
        AudioPath,
        TimingPoints,
        TimeSignaturePoints
    }
}