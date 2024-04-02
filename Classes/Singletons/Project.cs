using System;
using Godot;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Utility;

public partial class Project : Node
{
    [Export]
    private AudioStreamMP3 defaultMP3 = null!;

    private static Project instance = null!;

    private SongFile songFile = null!;
    public SongFile SongFile
    {
        get => (SongFile)SongPlayer.AudioFile;
        set
        {
            if (songFile == value)
                return;
            songFile = value;
            SongPlayer.AudioFile = songFile;
            //Signals.Instance.EmitEvent(Signals.Events.AudioFileChanged);
            Project.Instance.ProjectPath = null!;
        }
    }

    [Export]
    public AudioPlayer SongPlayer = null!;

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



    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
        SongFile = new SongFile(defaultMP3);
    }

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