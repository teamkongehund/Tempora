using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class LoadButton : Button {
    private FileDialog fileDialog;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        fileDialog = GetNode<FileDialog>("FileDialog");

        Pressed += OnPressed;
        fileDialog.FileSelected += OnFileSelected;
    }

    private void OnPressed() {
        fileDialog.CurrentDir = Settings.Instance.ProjectFilesDirectory;
        fileDialog.Popup();
    }
    //private void OnFileSelected(string selectedPath) => FileHandler.CopyFile(PathToCopyFrom, selectedPath);

    private void OnFileSelected(string selectedPath) {
        string extension = FileHandler.GetExtension(selectedPath);

        string correctExtension = ProjectFileManager.ProjectFileExtension;

        if (extension != correctExtension)
            return;

        ProjectFileManager.Instance.LoadProjectFromFilePath(selectedPath);

        string dir = FileHandler.GetDirectory(selectedPath);
        Settings.Instance.ProjectFilesDirectory = dir;
    }
}