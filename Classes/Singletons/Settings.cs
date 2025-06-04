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
    private static readonly string[] separator = ["\r\n", "\r", "\n"];
    private int numberOfBlocks = 10;
    private float measureOverlap;
    private bool metronomeFollowsGrid;
    private bool roundBPM;
    private int divisor = 1;
    private bool autoScrollWhenAddingTimingPoints = false;
    private string settingsPath = "user://settings.txt";
    private string projectFilesDirectory = "";
    private string oszFilesDirectory = "";
    private string beatSaberFilesDirectory = "";
    private string guitarGameFilesDirectory = "";
    private float downbeatPositionOffset = 0.125f;
    private bool seekPlaybackOnTimingPointChanges = true;
    private bool moveSubsequentTimingPointsWhenChangingTimeSignature = true;
    private double musicVolumeNormalized = 0.75f;
    private double metronomeVolumeNormalized = 1f;
    private double masterVolumeNormalized = 0.25f;
    private int beatsaberExportFormat = 4;

    public static Settings Instance { get => instance; set => instance = value; }
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
            SaveSettings();
        }
    }
    #endregion
    #region Files settings
    public string ProjectFilesDirectory
    {
        get => projectFilesDirectory;
        set
        {
            projectFilesDirectory = value;
            SaveSettings();
        }
    }
    public string OszFilesDirectory
    {
        get => oszFilesDirectory;
        set
        {
            oszFilesDirectory = value;
            SaveSettings();
        }
    }

    public string BeatSaberFilesDirectory
    {
        get => beatSaberFilesDirectory;
        set
        {
            beatSaberFilesDirectory = value;
            SaveSettings();
        }
    }

    public string GuitarGameFilesDirectory
    {
        get => guitarGameFilesDirectory;
        set
        {
            guitarGameFilesDirectory = value;
            SaveSettings();
        }
    }


    #endregion
    #region Timeline
    public readonly int MaxNumberOfBlocks = 30;
    /// <summary>
    ///     Number of waveform blocks to display
    /// </summary>
    public int NumberOfRows
    {
        get => numberOfBlocks;
        set
        {
            if (numberOfBlocks == value)
                return;
            numberOfBlocks = value;
            //GD.Print($"NumberOfBlocks changed to {numberOfBlocks}");
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
            SaveSettings();
        }
    }
    /// <summary>
    ///     How many measures of overlapping time is added to the beginning and end of each waveform block
    /// </summary>
    public float MeasureOverlap
    {
        get => measureOverlap;
        set
        {
            if (measureOverlap == value)
                return;
            measureOverlap = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
            SaveSettings();
        }
    }
    public float DownbeatPositionOffset
    {
        get => downbeatPositionOffset;
        set
        {
            if (downbeatPositionOffset == value)
                return;
            downbeatPositionOffset = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
            SaveSettings();
        }
    }
    #endregion
    public bool MetronomeFollowsGrid
    {
        get => metronomeFollowsGrid;
        set
        {
            if (metronomeFollowsGrid == value)
                return;
            metronomeFollowsGrid = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
            SaveSettings();
        }
    }
    public bool RoundBPM
    {
        get => roundBPM;
        set
        {
            if (roundBPM != value)
            {
                roundBPM = value;
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
                SaveSettings();
            }
        }
    }

    public bool SeekPlaybackOnTimingPointChanges
    {
        get => seekPlaybackOnTimingPointChanges;
        set
        {
            seekPlaybackOnTimingPointChanges = value;
            SaveSettings();
        }
    }
    public bool PreserveBPMWhenChangingTimeSignature
    {
        get => moveSubsequentTimingPointsWhenChangingTimeSignature;
        set
        {
            moveSubsequentTimingPointsWhenChangingTimeSignature = value;
            SaveSettings();
        }
    }
    public bool AutoScrollWhenAddingTimingPoints
    {
        get => autoScrollWhenAddingTimingPoints;
        set
        {
            autoScrollWhenAddingTimingPoints = value;
            SaveSettings();
        }
    }
    public int ExportOffsetMs = -14; // previously -29
    public bool MeasureResetsOnUnsupportedTimeSignatures = true;
    public bool RemovePointsThatChangeNothing = true;
    public bool AddExtraPointsOnDownbeats = true;
    public bool AddExtraPointsOnQuarterNotes = true;
    public bool OmitBarlines = true;
    public bool PreventDoubleBarlines = true;
    public bool ShowMoreSettings = false;
    public double MusicVolumeNormalized
    {
        get => musicVolumeNormalized;
        set
        {
            musicVolumeNormalized = value;
            SaveSettings();
        }
    }
    public double MetronomeVolumeNormalized
    {
        get => metronomeVolumeNormalized;
        set
        {
            metronomeVolumeNormalized = value;
            SaveSettings();
        }
    }
    public double MasterVolumeNormalized
    {
        get => masterVolumeNormalized;
        set
        {
            masterVolumeNormalized = value;
            SaveSettings();
        }
    }

    public int BeatSaberExportFormat
    {
        get => beatsaberExportFormat;
        set
        {
            beatsaberExportFormat = value;
            SaveSettings();
        }
    }
    private bool renderAsSpectrogram = true;
    public int SpectrogramStepSize { get => spectrogramStepSize; set => spectrogramStepSize = Math.Abs(value); }
    public int SpectrogramFftSize { get => spectrogramFftSize; set => spectrogramFftSize = Math.Abs(value); }
    public int SpectrogramMaxFreq { get => spectrogramMaxFreq; set => spectrogramMaxFreq = Math.Abs(value); }
    public int SpectrogramIntensity { get => spectrogramIntensity; set => spectrogramIntensity = Math.Abs(value); }
    public bool SpectrogramUseDb { get; set; } = true;
    public bool RenderAsSpectrogram
    {
        get => renderAsSpectrogram; set
        {
            renderAsSpectrogram = value;
            SaveSettings();
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.SettingsChanged));
        }
    }

    private enum Setting
    {
        ProjectFilesDirectory,
        OszFilesDirectory,
        BeatSaberFilesDirectory,
        GuitarGameFilesDirectory,
        GridDivisor,
        NumberOfRows,
        MeasureOverlap,
        DownbeatVisualOffset,
        MetronomeFollowsGrid,
        RoundBPM,
        MoveSubsequentTimingPointsWhenChangingTimeSignature,
        AutoScroll,
        SeekPlaybackOnTimingPointChanges,
        ExportOffsetMs,
        MeasureResetsOnUnsupportedTimeSignatures,
        RemovePointsThatChangeNothing,
        AddExtraPointsOnDownbeats,
        AddExtraPointsOnQuarterNotes,
        OmitBarlines,
        PreventDoubleBarLines,
        MusicVolumeNormalized,
        MetronomeVolumeNormalized,
        MasterVolumeNormalized,
        BeatSaberExportFormat,
        RenderAsSpectrogram,
        SpectrogramStepSize,
        SpectrogramFftSize,
        SpectrogramMaxFreq,
        SpectrogramIntensity,
        SpectrogramUseDb
    }
    private Dictionary<Setting, string> settingStrings = new()
       {
        {Setting.ProjectFilesDirectory, "ProjectFilesDirectory"},
        {Setting.OszFilesDirectory, "OszFilesDirectory"},
        {Setting.BeatSaberFilesDirectory, "BeatSaberFilesDirectory"},
        {Setting.GuitarGameFilesDirectory, "GuitarGameFilesDirectory"},
        {Setting.GridDivisor, "GridDivisor"},
        {Setting.NumberOfRows, "NumberOfRows"},
        {Setting.MeasureOverlap, "MeasureOverlap"},
        {Setting.DownbeatVisualOffset, "DownbeatVisualOffset"},
        {Setting.MetronomeFollowsGrid, "MetronomeFollowsGrid" },
        {Setting.RoundBPM, "RoundBPM" },
        {Setting.MoveSubsequentTimingPointsWhenChangingTimeSignature, "MoveSubsequentTimingPointsWhenChangingTimeSignature" },
        {Setting.AutoScroll, "AutoScroll" },
        {Setting.SeekPlaybackOnTimingPointChanges,"SeekPlaybackOnTimingPointChanges" },
        {Setting.ExportOffsetMs, "ExportOffsetMs" },
        {Setting.MeasureResetsOnUnsupportedTimeSignatures, "MeasureResetsOnUnsupportedTimeSignatures"},
        {Setting.RemovePointsThatChangeNothing, "RemovePointsThatChangeNothing"},
        {Setting.AddExtraPointsOnDownbeats, "AddExtraPointsOnDownbeats" },
        {Setting.AddExtraPointsOnQuarterNotes, "AddExtraPointsOnQuarterNotes" },
        {Setting.OmitBarlines, "OmitBarlines" },
        {Setting.PreventDoubleBarLines, "PreventDoubleBarLines" },
        {Setting.MusicVolumeNormalized, "MusicVolumeNormalized" },
        {Setting.MetronomeVolumeNormalized, "MetronomeVolumeNormalized" },
        {Setting.MasterVolumeNormalized, "MasterVolumeNormalized" },
        {Setting.BeatSaberExportFormat, "BeatSaberExportFormat" },
        {Setting.RenderAsSpectrogram, "RenderAsSpectrogram" },
        {Setting.SpectrogramStepSize, "SpectrogramStepSize"},
        {Setting.SpectrogramFftSize, "SpectrogramFftSize"},
        {Setting.SpectrogramMaxFreq, "SpectrogramMaxFreq"},
        {Setting.SpectrogramIntensity, "SpectrogramIntensity"},
        {Setting.SpectrogramUseDb, "SpectrogramUseDb" }
       };
    private int spectrogramIntensity = 5;
    private int spectrogramMaxFreq = 2200;
    private int spectrogramFftSize = 256;
    private int spectrogramStepSize = 64;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
        LoadSettings();
        ApplyVolumeSettingsToAudioServer();
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
                case var value when value == settingStrings[Setting.ProjectFilesDirectory]:
                    ProjectFilesDirectory = lineSplit[1];
                    break;
                case var value when value == settingStrings[Setting.OszFilesDirectory]:
                    OszFilesDirectory = lineSplit[1];
                    break;
                case var value when value == settingStrings[Setting.BeatSaberFilesDirectory]:
                    BeatSaberFilesDirectory = lineSplit[1];
                    break;
                case var value when value == settingStrings[Setting.GuitarGameFilesDirectory]:
                    GuitarGameFilesDirectory = lineSplit[1];
                    break;
                case var value when value == settingStrings[Setting.GridDivisor]:
                    _ = int.TryParse(lineSplit[1], out int parsedInt);
                    GridDivisor = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.NumberOfRows]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    NumberOfRows = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.MeasureOverlap]:
                    _ = float.TryParse(lineSplit[1], out float parsedFloat);
                    MeasureOverlap = parsedFloat;
                    break;
                case var value when value == settingStrings[Setting.DownbeatVisualOffset]:
                    _ = float.TryParse(lineSplit[1], out parsedFloat);
                    DownbeatPositionOffset = parsedFloat;
                    break;
                case var value when value == settingStrings[Setting.MetronomeFollowsGrid]:
                    _ = bool.TryParse(lineSplit[1], out bool parsedBool);
                    MetronomeFollowsGrid = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.RoundBPM]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    RoundBPM = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.MoveSubsequentTimingPointsWhenChangingTimeSignature]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    PreserveBPMWhenChangingTimeSignature = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.AutoScroll]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    AutoScrollWhenAddingTimingPoints = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.SeekPlaybackOnTimingPointChanges]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    SeekPlaybackOnTimingPointChanges = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.ExportOffsetMs]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    ExportOffsetMs = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.MeasureResetsOnUnsupportedTimeSignatures]:
                    _ = bool.TryParse(lineSplit[1], out MeasureResetsOnUnsupportedTimeSignatures);
                    break;
                case var value when value == settingStrings[Setting.RemovePointsThatChangeNothing]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    RemovePointsThatChangeNothing = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.AddExtraPointsOnDownbeats]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    AddExtraPointsOnDownbeats = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.AddExtraPointsOnQuarterNotes]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    AddExtraPointsOnQuarterNotes = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.OmitBarlines]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    OmitBarlines = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.PreventDoubleBarLines]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    PreventDoubleBarlines = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.MusicVolumeNormalized]:
                    _ = float.TryParse(lineSplit[1], out parsedFloat);
                    MusicVolumeNormalized = parsedFloat;
                    break;
                case var value when value == settingStrings[Setting.MetronomeVolumeNormalized]:
                    _ = float.TryParse(lineSplit[1], out parsedFloat);
                    MetronomeVolumeNormalized = parsedFloat;
                    break;
                case var value when value == settingStrings[Setting.MasterVolumeNormalized]:
                    _ = float.TryParse(lineSplit[1], out parsedFloat);
                    MasterVolumeNormalized = parsedFloat;
                    break;
                case var value when value == settingStrings[Setting.BeatSaberExportFormat]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    BeatSaberExportFormat = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.RenderAsSpectrogram]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    RenderAsSpectrogram = parsedBool;
                    break;
                case var value when value == settingStrings[Setting.SpectrogramStepSize]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    SpectrogramStepSize = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.SpectrogramFftSize]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    SpectrogramFftSize = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.SpectrogramMaxFreq]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    SpectrogramMaxFreq = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.SpectrogramIntensity]:
                    _ = int.TryParse(lineSplit[1], out parsedInt);
                    SpectrogramIntensity = parsedInt;
                    break;
                case var value when value == settingStrings[Setting.SpectrogramUseDb]:
                    _ = bool.TryParse(lineSplit[1], out parsedBool);
                    SpectrogramUseDb = parsedBool;
                    break;
            }
        }
    }
public void SaveSettings()
    {
        string settingsFile = "";
        settingsFile += GetSettingsFileLine(settingStrings[Setting.ProjectFilesDirectory], ProjectFilesDirectory);
        settingsFile += GetSettingsFileLine(settingStrings[Setting.OszFilesDirectory], OszFilesDirectory);
        settingsFile += GetSettingsFileLine(settingStrings[Setting.BeatSaberFilesDirectory], BeatSaberFilesDirectory);
        settingsFile += GetSettingsFileLine(settingStrings[Setting.GuitarGameFilesDirectory], GuitarGameFilesDirectory);
        settingsFile += GetSettingsFileLine(settingStrings[Setting.GridDivisor], GridDivisor.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.NumberOfRows], NumberOfRows.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MeasureOverlap], MeasureOverlap.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.DownbeatVisualOffset], DownbeatPositionOffset.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MetronomeFollowsGrid], MetronomeFollowsGrid.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.RoundBPM], RoundBPM.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MoveSubsequentTimingPointsWhenChangingTimeSignature], PreserveBPMWhenChangingTimeSignature.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.AutoScroll], AutoScrollWhenAddingTimingPoints.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.SeekPlaybackOnTimingPointChanges], SeekPlaybackOnTimingPointChanges.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.ExportOffsetMs], ExportOffsetMs.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MeasureResetsOnUnsupportedTimeSignatures], MeasureResetsOnUnsupportedTimeSignatures.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.RemovePointsThatChangeNothing], RemovePointsThatChangeNothing.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.AddExtraPointsOnDownbeats], AddExtraPointsOnDownbeats.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.AddExtraPointsOnQuarterNotes], AddExtraPointsOnQuarterNotes.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.OmitBarlines], OmitBarlines.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.PreventDoubleBarLines], PreventDoubleBarlines.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MusicVolumeNormalized], MusicVolumeNormalized.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MetronomeVolumeNormalized], MetronomeVolumeNormalized.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.MasterVolumeNormalized], MasterVolumeNormalized.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.BeatSaberExportFormat], BeatSaberExportFormat.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.RenderAsSpectrogram], RenderAsSpectrogram.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.SpectrogramStepSize], SpectrogramStepSize.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.SpectrogramFftSize], SpectrogramFftSize.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.SpectrogramMaxFreq], SpectrogramMaxFreq.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.SpectrogramIntensity], SpectrogramIntensity.ToString());
        settingsFile += GetSettingsFileLine(settingStrings[Setting.SpectrogramUseDb], SpectrogramUseDb.ToString());
        FileHandler.SaveText(settingsPath, settingsFile);
    }

    private string GetSettingsFileLine(string setting, string value) => setting + ";" + value + "\n";

    public static int DivisorToSliderValue(int divisor) => GridSliderToDivisorDict.FirstOrDefault(x => x.Value == divisor).Key;

    private void ApplyVolumeSettingsToAudioServer()
    {
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Music"),
            Mathf.LinearToDb((float)MusicVolumeNormalized)
            );
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Metronome"),
            Mathf.LinearToDb((float)MetronomeVolumeNormalized)
            );
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Master"),
            Mathf.LinearToDb((float)MasterVolumeNormalized)
            );
    }
}
