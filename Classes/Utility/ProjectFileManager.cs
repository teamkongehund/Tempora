using Godot;
using System;
using System.Globalization;


/// <summary>
/// Handles saving and loading of project files
/// </summary>
public partial class ProjectFileManager : Node
{
	public static ProjectFileManager Instance;
	Settings Settings;

	public static readonly string ProjectFileExtension = "tmpr"; 

    private enum ParseMode
    {
        None,
		AudioPath,
        TimingPoints,
		TimeSignaturePoints
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Instance = this;
		Settings = Settings.Instance;
	}

	public void SaveProjectAs(string filePath)
	{
		string file = GetProjectAsString();
		FileHandler.SaveText(filePath, file);
	}

	public string GetProjectAsString()
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

		// TimingPoint
		// Time;MusicPosition;TimeSignatureUpper;TimeSignatureLower
		string timingPointsLines = "";
		foreach (TimingPoint timingPoint in Timing.Instance.TimingPoints)
		{
			string timingPointLine = "";
			timingPointLine += timingPoint.Time.ToString(CultureInfo.InvariantCulture);
			timingPointLine += ";";
            timingPointLine += ((float)timingPoint.MusicPosition).ToString(CultureInfo.InvariantCulture);
            timingPointLine += ";";
            timingPointLine += timingPoint.TimeSignature[0].ToString();
            timingPointLine += ";";
            timingPointLine += timingPoint.TimeSignature[1].ToString();
            timingPointLine += "\n";

			timingPointsLines += timingPointLine;
        }

		string file = "";
        file += "[AudioPath]\n";
		file += Project.Instance.AudioFile.Path + "\n";
        file += "[TimeSignaturePoints]\n"; 
		file += timeSignaturePointsLines;
		file += "[TimingPoints]\n";
		file += timingPointsLines;

		return file;
	}

	public void LoadProjectFromFile(string projectFile)
	{
		Timing.Instance = new Timing();
		Timing.Instance.IsInstantiating = true;

        string[] lines = projectFile.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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
                default:
                    break;
            }

            string[] lineData = line.Split(";", StringSplitOptions.None);

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
                        ) continue;

					Timing.Instance.UpdateTimeSignature(new int[] { timeSignatureUpper, timeSignatureLower }, tsMusicPosition);

                    break;
				case ParseMode.TimingPoints:
					if (lineData.Length != 4)
						continue;
					
					bool timeParsed = float.TryParse(
						lineData[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float time);
					bool tpMusicPositionParsed = float.TryParse(
						lineData[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float tpMusicPosition);
					timeSignatureUpperParsed = int.TryParse(lineData[2], out timeSignatureUpper);
                    timeSignatureLowerParsed = int.TryParse(lineData[3], out timeSignatureLower);
					if (timeParsed == false
						|| tpMusicPositionParsed == false
						|| timeSignatureUpperParsed == false
						|| timeSignatureLowerParsed == false
						) continue;

					Timing.Instance.AddTimingPoint(tpMusicPosition, time);

					break;
				default:
					break;
			}
        }

		Project.Instance.AudioFile = new AudioFile(audioPath);
		Timing.Instance.IsInstantiating = false;
		Signals.Instance.EmitSignal("TimingChanged");
    }

	public void LoadProjectFromFilePath(string filePath)
	{
		string projectFile = FileHandler.LoadText(filePath);
		if (string.IsNullOrEmpty(projectFile))
			return;
		LoadProjectFromFile(projectFile);
	}
}
