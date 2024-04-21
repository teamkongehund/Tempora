using Godot;
using NAudio.SoundFont;
using System;
using System.Linq;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using System.IO;

namespace Tempora.Classes.Visual;
public partial class FileMenu : PopupMenu
{
    [Export]
    private FileDialog saveFileDialog = null!;

    [Export]
    private FileDialog loadFileDialog = null!;

    [Export]
    private MusicPlayer audioPlayer = null!;

    private enum SaveConfig
    {
        project,
        osz
    }

    private SaveConfig latestSaveConfig = SaveConfig.project;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        IdPressed += OnIdPressed;
        saveFileDialog.FileSelected += OnSaveFilePathSelected;
        loadFileDialog.FileSelected += OnLoadFilePathSelected;
    }

    private void OnIdPressed(long id)
    {
        switch (id)
        {
            case 0:
                LoadFileDialogPopup();
                break;
            case 1:
                SaveFileDialogPopup(SaveConfig.project);
                break;
            case 2:
                OsuExporter.ExportAndOpenOsz();
                break;
            case 3:
                SaveFileDialogPopup(SaveConfig.osz);
                break;
        }
    }

    #region Save File
    private void SaveFileDialogPopup(SaveConfig config)
    {
        switch (config)
        {
            case SaveConfig.project:
                saveFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
                break;
            case SaveConfig.osz:
                saveFileDialog.CurrentDir = Settings.Instance.OszFilesDirectory;
                break;
        }
        latestSaveConfig = config;
        saveFileDialog.Popup();
    }
    private void OnSaveFilePathSelected(string selectedPath)
    {
        switch (latestSaveConfig)
        {
            case SaveConfig.project:
                ProjectFileManager.SaveProjectAs(selectedPath);
                string dir = FileHandler.GetDirectory(selectedPath);
                Settings.Instance.ProjectFilesDirectory = dir;
                break;
            case SaveConfig.osz:
                OsuExporter.SaveOszAs_AndShowInFileExplorer(selectedPath);
                dir = FileHandler.GetDirectory(selectedPath);
                Settings.Instance.OszFilesDirectory = dir;
                break;
        }
    }
    #endregion

    #region Load File
    private void LoadFileDialogPopup()
    {
        loadFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        loadFileDialog.Popup();
    }

    private void OnLoadFilePathSelected(string selectedPath)
    {
        string extension = FileHandler.GetExtension(selectedPath);

        string projectFileExtension = ProjectFileManager.ProjectFileExtension;
        string mp3Extension = "mp3";

        string[] allowedExtensions =
        [
            projectFileExtension,
            mp3Extension
        ];

        if (!allowedExtensions.Contains(extension))
            return;

        switch (extension)
        {
            case var value when value == mp3Extension:
                var audioFile = new AudioFile(selectedPath);
                Project.Instance.AudioFile = audioFile;
                audioPlayer.LoadMp3();
                break;
            case var value when value == projectFileExtension:
                ProjectFileManager.Instance.LoadProjectFromFilePath(selectedPath);
                break;
        }

        string dir = FileHandler.GetDirectory(selectedPath);
        Settings.Instance.ProjectFilesDirectory = dir;
    } 
    #endregion


}
