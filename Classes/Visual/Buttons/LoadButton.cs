using System;
using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class LoadButton : Button
{
    private AudioPlayer audioPlayer = null!;
    
    [Export]
    private FileDialog loadFileDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        audioPlayer = Project.Instance.SongPlayer;
        Pressed += OnPressed;
        loadFileDialog.FileSelected += OnFileSelected;
    }

    private void OnPressed()
    {
        loadFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        loadFileDialog.Popup();
    }
    //private void OnFileSelected(string selectedPath) => FileHandler.CopyFile(PathToCopyFrom, selectedPath);

    private void OnFileSelected(string selectedPath)
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
                var songFile = new SongFile(selectedPath);
                Project.Instance.SongFile = songFile;
                audioPlayer.LoadStream();
                break;
            case var value when value == projectFileExtension:
                ProjectFileManager.Instance.LoadProjectFromFilePath(selectedPath);
                break;
        }

        string dir = FileHandler.GetDirectory(selectedPath);
        Settings.Instance.ProjectFilesDirectory = dir;
    }
}