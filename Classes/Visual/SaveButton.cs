using Godot;
using System;

public partial class SaveButton : Button
{
    private FileDialog FileDialog;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        FileDialog = GetNode<FileDialog>("FileDialog");

        //ProjectFileManager.Instance.SaveProjectAs("user://savedProject.txt")

        Pressed += OnPressed;
        FileDialog.FileSelected += OnFileSelected;
    }

    private void OnPressed()
    {
        FileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        FileDialog.Popup();
    }
    //private void OnFileSelected(string selectedPath) => FileHandler.CopyFile(PathToCopyFrom, selectedPath);

    private void OnFileSelected(string selectedPath)
    {
        string extension = FileHandler.GetExtension(selectedPath);

        string correctExtension = ProjectFileManager.ProjectFileExtension;

        if (extension != correctExtension)
            selectedPath += "." + correctExtension;

        ProjectFileManager.Instance.SaveProjectAs(selectedPath);

        string dir = FileHandler.GetDirectory(selectedPath);
        Settings.Instance.ProjectFilesDirectory = dir;
    }
}
