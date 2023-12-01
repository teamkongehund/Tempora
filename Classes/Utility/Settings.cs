using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Settings : Node
{
	public static Settings Instance;

    private string SettingsPath = "user://settings.txt";

	private string _projectFilesDirectory = "";
	public string ProjectFilesDirectory
	{
		get => _projectFilesDirectory;
		set
		{
			_projectFilesDirectory = value;
			SaveSettings();
		}
	}

    private int _divisor = 4;
	/// <summary>
	/// Musical grid divisor - can be thought of as 1/Divisor - i.e. a value of 4 means "display quarter notes"
	/// </summary>
	public int Divisor
	{
		get => _divisor;
		set
		{
			if (_divisor == value) return;
			_divisor = value;
            Signals.Instance.EmitSignal("SettingsChanged");
        }
	}

	public static readonly Dictionary<int, int> SliderToDivisorDict = new Dictionary<int, int>()
	{
		{ 1, 1 },
		{ 2, 2 },
		{ 3, 3 },
		{ 4, 4 },
		{ 5, 6 },
		{ 6, 8 },
		{ 7, 12 },
        { 8, 16 },
    };

	// TODO 2: Add support for any grid division with a field entry.

	public static int DivisorToSlider(int divisor) => SliderToDivisorDict.FirstOrDefault(x => x.Value == divisor).Key;

	private int _numberOfBlocks = 10 ;
	/// <summary>
	/// Number of waveform blocks to display
	/// </summary>
	public int NumberOfBlocks
	{
		get => _numberOfBlocks;
		set
		{
			if (_numberOfBlocks == value) return;
			_numberOfBlocks = value;
			Signals.Instance.EmitSignal("SettingsChanged");
		}
	}

	/// <summary>
	/// Should not be changed during runtime!
	/// </summary>
	public int MaxNumberOfBlocks = 20;

	private float _musicPositionMargin = 0f;
    /// <summary>
    /// How many measures of overlapping time is added to the beginning and end of each waveform block
    /// </summary>
    public float MusicPositionMargin
	{
		get => _musicPositionMargin;
		set
		{
			if (_musicPositionMargin == value) return;
			_musicPositionMargin = value;
            Signals.Instance.EmitSignal("SettingsChanged");
        }
	}

	private float _musicPositionOffset = 0f;

    public float MusicPositionOffset
	{
		get => _musicPositionOffset;
		set
		{
			if (_musicPositionOffset == value) return;
			_musicPositionOffset = value;
            Signals.Instance.EmitSignal("SettingsChanged");
        }
	}

	/// <summary>
	/// Snap timing points to beat grid when moving them. 
	/// Should always be true because osu's timing points are always on-grid. 
	/// Probably best to exclude from any settings menu beacuse of this.
	/// </summary>
	public bool SnapToGridEnabled = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		LoadSettings();
    }

	public void LoadSettings()
	{
		if (string.IsNullOrEmpty(SettingsPath))
			return;
		string settingsFile = FileHandler.LoadText(SettingsPath);
		string[] lines = settingsFile.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			string[] lineSplit = line.Split(";", StringSplitOptions.None);
			if (lineSplit.Length != 2 || lineSplit[1] == "") 
				continue;

			switch (lineSplit[0])
			{
				case "ProjectFilesDirectory":
					ProjectFilesDirectory = lineSplit[1];
					break;
				default:
					break;
			}
		}
	}

	public void SaveSettings()
	{
		string settingsFile = "";
		settingsFile += "ProjectFilesDirectory" + ";" + ProjectFilesDirectory + "\n";
		FileHandler.SaveText(SettingsPath, settingsFile);
    }
}
