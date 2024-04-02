using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GD = Tempora.Classes.DataTools.GD;

namespace Tempora.Classes.Utility;

public partial class Settings : Node
{
    private static Settings instance = null!;

    public static readonly Dictionary<int, int> SliderToDivisorDict = new() {
        { 1, 1 },
        { 2, 2 },
        { 3, 3 },
        { 4, 4 },
        { 5, 6 },
        { 6, 8 },
        { 7, 12 },
        { 8, 16 }
    };

    private int divisor = 4;

    public readonly int MaxNumberOfRows = 60;

    private float musicPositionMargin;

    private float musicPositionOffset = 0.125f;


    private string projectFilesDirectory = "";

    private string settingsPath = "user://settings.txt";

    /// <summary>
    ///     Snap timing points to beat grid when moving them.
    ///     Should always be true because osu's timing points are always on-grid.
    ///     Probably best to exclude from any settings menu beacuse of this.
    /// </summary>
    public bool SnapToGridEnabled = true;

    public string ProjectFilesDirectory
    {
        get => projectFilesDirectory;
        set
        {
            projectFilesDirectory = value;
            SaveSettings();
        }
    }

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
            Signals.Instance.EmitEvent(Signals.Events.SettingsChanged);
        }
    }

    private int numberOfRows = 10;
    /// <summary>
    ///     Number of waveform blocks to display
    /// </summary>
    public int NumberOfRows
    {
        get => numberOfRows;
        set
        {
            if (numberOfRows == value)
                return;
            numberOfRows = value;
            //GD.Print($"NumberOfBlocks changed to {numberOfBlocks}");
            Signals.Instance.EmitEvent(Signals.Events.SettingsChanged);
        }
    }

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
            Signals.Instance.EmitEvent(Signals.Events.SettingsChanged);
        }
    }

    public float MusicPositionOffset
    {
        get => musicPositionOffset;
        set
        {
            if (musicPositionOffset == value)
                return;
            musicPositionOffset = value;
            Signals.Instance.EmitEvent(Signals.Events.SettingsChanged);
        }
    }

    public static Settings Instance { get => instance; set => instance = value; }

    private static readonly string[] separator = ["\r\n", "\r", "\n"];

    public bool MetronomeFollowsGrid = false;

    public bool PreserveBpmOnTimeSignatureChanges = true;

    public static int DivisorToSlider(int divisor) => SliderToDivisorDict.FirstOrDefault(x => x.Value == divisor).Key;

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
            }
        }
    }

    public void SaveSettings()
    {
        string settingsFile = "";
        settingsFile += "ProjectFilesDirectory" + ";" + ProjectFilesDirectory + "\n";
        FileHandler.SaveText(settingsPath, settingsFile);
    }
}