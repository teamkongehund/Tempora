using System;
using System.IO;
using Godot;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Utility;

/// <summary>
/// Contains data about the current project.
/// </summary>
public partial class Project : Node
{
    private static Project instance = null!;

    private AudioFile audioFile = null!;

    private Settings settings = null!;

    private string? projectPath = null;
    public string? ProjectPath
    {
        get => projectPath;
        set
        {
            if (value == projectPath)
                return;
            projectPath = value;
            string? projectName = Path.GetFileName(projectPath);
            string titleAddition = projectName != null ? $" - {projectName}" : "";
            GetWindow().Title = "Tempora" + titleAddition;
        }
    }

    public event EventHandler NotificationMessageChanged = null!;
    private string notificationMessage = null!;
    public string NotificationMessage
    {
        get => notificationMessage;
        set
        {
            notificationMessage = value;
            NotificationMessageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public static Project Instance { get => instance; set => instance = value; }

    public AudioFile AudioFile
    {
        get => audioFile;
        set
        {
            if (audioFile == value)
                return;
            audioFile = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Instance.AudioFileChanged), this, EventArgs.Empty);
            Project.Instance.ProjectPath = null!;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Instance = this;
}