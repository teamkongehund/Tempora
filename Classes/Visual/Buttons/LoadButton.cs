using System;
using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class LoadButton : Button
{
    [Export]
    AudioPlayer audioPlayer = null!;

    private FileDialog fileDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        fileDialog = GetNode<FileDialog>("FileDialog");

        Pressed += OnPressed;
        fileDialog.FileSelected += OnFileSelected;
    }

    private void OnPressed()
    {
        fileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        fileDialog.Popup();
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
}