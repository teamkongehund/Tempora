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
using System.Linq;
using Godot;
using GD = Tempora.Classes.DataHelpers.GD;

namespace Tempora.Classes.Utility;

public partial class Settings : Node
{
	private static Settings instance = null!;
	public static Settings Instance { get => instance; set => instance = value; }
	private static readonly string[] separator = ["\r\n", "\r", "\n"];

	public static readonly Dictionary<int, int> GridSliderToDivisorDict = new() {
		{ 1, 1 },
		{ 2, 2 },
		{ 3, 3 },
		{ 4, 4 },
		{ 5, 6 },
		{ 6, 8 },
		{ 7, 12 },
		{ 8, 16 }
	};

	#region Grid
	private int divisor = 1;
	public static int DivisorToSliderValue(int divisor) => GridSliderToDivisorDict.FirstOrDefault(x => x.Value == divisor).Key;
	/// <summary>
	///     Snap timing points to beat grid when moving them.
	///     Should always be true because osu's timing points are always on-grid.
	///     Probably best to exclude from any settings menu beacuse of this.
	/// </summary>
	public bool SnapToGridEnabled = true; 
	/// <summary>
	///     Musical grid divisor - can be thought of as 1/Divisor - i.e. a value of 4 means "display quarter notes"
	/// </summary>
	public int GridDivisor
	{
		get => divisor;
		set
		{
			if (divisor == value)
				return;
			divisor = value;
			GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
		}
	}
	#endregion

	#region Files settings
	private string settingsPath = "user://settings.txt";
	private string projectFilesDirectory = "";
	public string ProjectFilesDirectory
	{
		get => projectFilesDirectory;
		set
		{
			projectFilesDirectory = value;
			SaveSettings();
		}
	}
	private string oszFilesDirectory = "";
	public string OszFilesDirectory
	{
		get => oszFilesDirectory;
		set
		{
			oszFilesDirectory = value;
			SaveSettings();
		}
	}
	#endregion

	#region Timeline
	public readonly int MaxNumberOfBlocks = 30;
	private int numberOfBlocks = 10;
	/// <summary>
	///     Number of waveform blocks to display
	/// </summary>
	public int NumberOfBlocks
	{
		get => numberOfBlocks;
		set
		{
			if (numberOfBlocks == value)
				return;
			numberOfBlocks = value;
			//GD.Print($"NumberOfBlocks changed to {numberOfBlocks}");
			GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
		}
	}
	private float musicPositionMargin;
	/// <summary>
	///     How many measures of overlapping time is added to the beginning and end of each waveform block
	/// </summary>
	public float MusicPositionMargin
	{
		get => musicPositionMargin;
		set
		{
			if (musicPositionMargin == value)
				return;
			musicPositionMargin = value;
			GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
		}
	}
	private float musicPositionOffset = 0.125f;
	public float MusicPositionOffset
	{
		get => musicPositionOffset;
		set
		{
			if (musicPositionOffset == value)
				return;
			musicPositionOffset = value;
			GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
		}
	}
	#endregion

	private bool metronomeFollowsGrid;

	public bool MetronomeFollowsGrid
	{
		get => metronomeFollowsGrid;
		set
		{
			if (metronomeFollowsGrid == value)
				return;
			metronomeFollowsGrid = value;
			GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
		}
	}
	
	//code from binarie -> Rounding BPM
	
	private bool roundBPM;
	
	public bool RoundBPM
	{
		get => roundBPM;
		set
		{
			if (roundBPM != value)
			{
				roundBPM = value;
				GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
			}
		}
	}
	public bool MoveSubsequentTimingPointsWhenChangingTimeSignature = true;

	public bool AutoScrollWhenAddingTimingPoints = false;

    public bool SeekPlaybackOnTimingPointChanges = true;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		LoadSettings();
	}

	public void LoadSettings()
	{
		if (string.IsNullOrEmpty(settingsPath))
			return;
		string settingsFile;
		try
		{
			settingsFile = FileHandler.LoadText(settingsPath);
		}
		catch
		{
			GD.Print($"Failed to load {settingsPath}: No settings file saved in user folder.");
			return;
		}
		string[] lines = settingsFile.Split(separator, StringSplitOptions.None);
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			string[] lineSplit = line.Split(";");
			if (lineSplit.Length != 2 || lineSplit[1] == "")
				continue;

			switch (lineSplit[0])
			{
				case "ProjectFilesDirectory":
					ProjectFilesDirectory = lineSplit[1];
					break;
				case "OszFilesDirectory":
					OszFilesDirectory = lineSplit[1];
					break;
			}
		}
	}

	public void SaveSettings()
	{
		string settingsFile = "";
		settingsFile += "ProjectFilesDirectory" + ";" + ProjectFilesDirectory + "\n";
		settingsFile += "OszFilesDirectory" + ";" + OszFilesDirectory + "\n";
		FileHandler.SaveText(settingsPath, settingsFile);
	}
}
