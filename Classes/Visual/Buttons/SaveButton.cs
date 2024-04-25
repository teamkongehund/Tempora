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