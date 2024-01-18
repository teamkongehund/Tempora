using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class SaveButton : Button
{
    private FileDialog fileDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        fileDialog = GetNode<FileDialog>("FileDialog");

        //ProjectFileManager.Instance.SaveProjectAs("user://savedProject.txt")

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
        ProjectFileManager.Instance.SaveProjectAs(selectedPath);

        string dir = FileHandler.GetDirectory(selectedPath);
        Settings.Instance.ProjectFilesDirectory = dir;
    }
}