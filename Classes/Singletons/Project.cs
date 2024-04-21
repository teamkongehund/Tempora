using System;
using Godot;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Utility;

public partial class Project : Node
{
    private static Project instance = null!;

    private AudioFile audioFile = null!;

    private Settings settings = null!;

    public string ProjectPath = null!;

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

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey keyEvent:
                {
                    if (keyEvent.Keycode == Key.S && keyEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
                        ProjectFileManager.SaveProjectAs(ProjectPath);
                    break;
                }
        }
    }
}