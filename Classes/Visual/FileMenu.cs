using Godot;
using NAudio.SoundFont;
using System;
using System.Linq;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

public partial class FileMenu : PopupMenu
{
    [Export]
    private FileDialog saveFileDialog = null!;

    [Export]
    private FileDialog loadFileDialog = null!;

    [Export]
    private AudioPlayer audioPlayer = null!;

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
                SaveFileDialogPopup();
                break;
            case 3:
                OsuExporter.ExportOsz();
                break;
        }
    }

    #region Save File
    private void SaveFileDialogPopup()
    {
        saveFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        saveFileDialog.Popup();
    }
    private void OnSaveFilePathSelected(string selectedPath)
    {
        ProjectFileManager.SaveProjectAs(selectedPath);

        string dir = FileHandler.GetDirectory(selectedPath);
        Settings.Instance.ProjectFilesDirectory = dir;
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
