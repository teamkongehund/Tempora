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

using System.IO;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class SaveButton : Button
{
    [Export]
    private FileDialog saveFileDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Pressed += OnPressed;
        saveFileDialog.FileSelected += OnFileSelected;
    }

    private void OnPressed()
    {
        saveFileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        saveFileDialog.Popup();
    }
    //private void OnFileSelected(string selectedPath) => FileHandler.CopyFile(PathToCopyFrom, selectedPath);

    private void OnFileSelected(string selectedPath)
    {
        ProjectFileManager.SaveProjectAs(selectedPath);

        string dir = Path.GetDirectoryName(selectedPath) ?? "";
        Settings.Instance.ProjectFilesDirectory = dir;
    }
}