// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share — copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution — You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial — You may not use the material for commercial purposes.
// - NoDerivatives — If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using System.IO;
using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class LoadButton : Button
{
    MusicPlayer MusicPlayer => MusicPlayer.Instance;

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
        string extension = Path.GetExtension(selectedPath).ToLower();

        string projectFileExtension = ProjectFileManager.ProjectFileExtension;
        string mp3Extension = ".mp3";
        string oggExtension = ".ogg";

        string[] allowedExtensions =
        [
            projectFileExtension,
            mp3Extension,
            oggExtension
        ];

        if (!allowedExtensions.Contains(extension))
            return;

        switch (extension)
        {
            case var value when value == mp3Extension:
                var audioFile = new AudioFile(selectedPath);
                Project.Instance.AudioFile = audioFile;
                break;
            case var value when value == oggExtension:
                audioFile = new AudioFile(selectedPath);
                Project.Instance.AudioFile = audioFile;
                break;
            case var value when value == projectFileExtension:
                ProjectFileManager.Instance.LoadProjectFromFilePath(selectedPath);
                break;
        }

        string dir = Path.GetDirectoryName(selectedPath) ?? "";
        Settings.Instance.ProjectFilesDirectory = dir;
    }
}