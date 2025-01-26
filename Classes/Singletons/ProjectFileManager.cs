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
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Utility;

/// <summary>
///     Handles saving and loading of project files
/// </summary>
public partial class ProjectFileManager : Node
{
    #region Properties & Fields
    private static ProjectFileManager instance = null!;

    [Export]
    public FileDialog SaveFileDialog = null!;

    [Export]
    public FileDialog LoadFileDialog = null!;

    private MusicPlayer MusicPlayer => MusicPlayer.Instance;

    public static readonly string ProjectFileExtension = ".tmpr";
    private Settings settings => Settings.Instance;
    private static readonly string[] separator = ["\r\n", "\r", "\n"];

    public const string AutoSavePath = "user://autosave";

    public static ProjectFileManager Instance { get => instance; set => instance = value; } 
    #endregion

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;

        SaveFileDialog.FileSelected += OnSaveFilePathSelected;
        LoadFileDialog.FileSelected += OnLoadFilePathSelected;
        GlobalEvents.Instance.TimingChanged += OnTimingChanged;
    }

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey keyEvent:
                {
                    if (keyEvent.Keycode == Key.S && keyEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
                        ProjectFileManager.SaveProjectAs(Project.Instance.ProjectPath);
                    break;
                }
        }
    }

    #region Save Dialog

    public enum SaveConfig
    {
        project,
        osz
    }

    private SaveConfig latestSaveConfig = SaveConfig.project;
    public void SaveOszFileDialogPopup() => SaveFileDialogPopup(SaveConfig.osz);
    public void SaveProjectFileDialogPopup() => SaveFileDialogPopup(SaveConfig.project);
    private void SaveFileDialogPopup(SaveConfig config)
    {
        switch (config)
        {
            case SaveConfig.project:
                SaveFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
                SaveFileDialog.Title = "Save Project";
                break;
            case SaveConfig.osz:
                SaveFileDialog.CurrentDir = Settings.Instance.OszFilesDirectory;
                SaveFileDialog.Title = "Export osz";
                break;
        }
        latestSaveConfig = config;
        SaveFileDialog.Popup();
    }
    private void OnSaveFilePathSelected(string selectedPath)
    {
        switch (latestSaveConfig)
        {
            case SaveConfig.project:
                ProjectFileManager.SaveProjectAs(selectedPath);
                string dir = Path.GetDirectoryName(selectedPath) ?? "";
                Settings.Instance.ProjectFilesDirectory = dir;
                break;
            case SaveConfig.osz:
                OsuExporter.Instance.SaveOszAs_AndShowInFileExplorer(selectedPath);
                dir = Path.GetDirectoryName(selectedPath) ?? "";
                Settings.Instance.OszFilesDirectory = dir;
                break;
        }
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        if (Timing.Instance.IsInstantiating)
            return;
        AutoSave();
    }
    #endregion

    #region Load Dialog
    public void LoadFileDialogPopup()
    {
        LoadFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        LoadFileDialog.Popup();
    }

    private void OnLoadFilePathSelected(string selectedPath)
    {
        string extension = Path.GetExtension(selectedPath).ToLower();

        string projectFileExtension = ProjectFileExtension;
        string mp3Extension = ".mp3";
        string oggExtension = ".ogg";

        string[] allowedExtensions =
        [
            projectFileExtension,
            mp3Extension, 
            oggExtension,
        ];

        if (!allowedExtensions.Contains(extension))
            return;

        switch (extension)
        {
            case var value when (value == mp3Extension || value == oggExtension):
                var audioFile = new AudioFile(selectedPath);
                Project.Instance.AudioFile = audioFile;
                break;
            case var value when value == projectFileExtension:
                ProjectFileManager.Instance.LoadProjectFromFilePath(selectedPath);
                Project.Instance.NotificationMessage = $"Loaded {selectedPath}";
                string dir = Path.GetDirectoryName(selectedPath) ?? "";
                Settings.Instance.ProjectFilesDirectory = dir;
                break;
        }
    }
    #endregion

    #region Save Project
    public static void SaveProjectAs(string? filePath)
    {
        if (filePath == null)
        {
            Instance.SaveProjectFileDialogPopup();
            return;
        }

        SaveProject(filePath);

        Project.Instance.ProjectPath = filePath;
        Project.Instance.NotificationMessage = $"Saved to {filePath}";
    }

    private static void SaveProject(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        string correctExtension = ProjectFileManager.ProjectFileExtension;

        filePath = Path.ChangeExtension(filePath, correctExtension);
        string? fileDir = Path.GetDirectoryName(filePath) ?? throw new NullReferenceException(nameof(filePath));
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        string audioFileExtension = Project.Instance.AudioFile.Extension;
        string audioFilePathShort = $"{fileName}{audioFileExtension}";
        string audioFilePathLong = fileDir == "user:" 
            ? fileDir + "//" + audioFilePathShort 
            : Path.Combine(fileDir, audioFilePathShort);
        
        using var audioFile = Godot.FileAccess.Open(audioFilePathLong, Godot.FileAccess.ModeFlags.Write);
        var error = Godot.FileAccess.GetOpenError();
        audioFile.StoreBuffer(Project.Instance.AudioFile.FileBuffer);

        string file = CreateProjectFileString(audioFilePathShort);
        FileHandler.SaveText(filePath, file);
    }

    public static void AutoSave() => SaveProject(AutoSavePath);

    public static string CreateProjectFileString() => CreateProjectFileString(Project.Instance.AudioFile.FilePath);

    public static string CreateProjectFileString(string audioPath)
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

    private static string GetTimingPointsAsString()
    {
        // Time;MusicPosition;TimeSignatureUpper;TimeSignatureLower
        string timingPointsLines = "";
        TimingPoint lastTimingPoint = Timing.Instance.TimingPoints[^1];
        foreach (TimingPoint timingPoint in Timing.Instance.TimingPoints)
        {
            if (timingPoint?.MusicPosition == null)
                continue;
            string timingPointLine = "";
            timingPointLine += timingPoint.Offset.ToString(CultureInfo.InvariantCulture);
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
    #endregion

    #region Load Project
    public void NewProject()
    {
        Project.Instance.ProjectPath = null;
        Project.Instance.NotificationMessage = "You are now editing a new project.";
    }

    private void LoadProjectFromFile(string projectFile, string filePath)
    {
        Timing.Instance = new Timing
        {
            IsInstantiating = true
        };

        string[] lines = projectFile.Split(separator, StringSplitOptions.None);
        string audioPath = "";
        string? fileDir = Path.GetDirectoryName(filePath) ?? throw new NullReferenceException("filePath was null");
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
                    audioPath = Path.Combine(fileDir, line);
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

                    Timing.Instance.UpdateTimeSignature([timeSignatureUpper, timeSignatureLower], tsMusicPosition);

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
        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
    }

    public void LoadProjectFromFilePath(string filePath)
    {
        string projectFile = FileHandler.LoadText(filePath);
        if (string.IsNullOrEmpty(projectFile))
            return;
        if (Project.Instance.AudioFile != null)
            MusicPlayer.Instance.Pause();
        LoadProjectFromFile(projectFile, filePath);
        Project.Instance.ProjectPath = filePath;
    }

    private enum ParseMode
    {
        None,
        AudioPath,
        TimingPoints,
        TimeSignaturePoints
    }  
    #endregion
}