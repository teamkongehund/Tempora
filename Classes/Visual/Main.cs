using System;
using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

// Tempora

namespace Tempora.Classes.Visual;

public partial class Main : Control
{
    //[Export]
    //private string audioPath = "res://Audio/UMO.mp3";
    [Export]
    private AudioStreamMP3 defaultMP3 = null!;
    private MusicPlayer MusicPlayer => MusicPlayer.Instance;
    [Export]
    private AudioVisualsContainer audioVisualsContainer = null!;
    [Export]
    private Metronome metronome = null!;
    [Export]
    private BlockScrollBar blockScrollBar = null!;

    private ProjectFileManager projectFileManager = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        projectFileManager = ProjectFileManager.Instance;

        // This works in Debug if we use i.e. audioPath = "res://Audio/UMO.mp3",
        // but won't work in production, as resources are converted to different file formats.
        //Project.Instance.AudioFile = new AudioFile(audioPath);

        Project.Instance.AudioFile = new AudioFile(defaultMP3);

        GlobalEvents.Instance.Scrolled += OnScrolled;
        GlobalEvents.Instance.SettingsChanged += OnSettingsChanged;
        audioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
        GetTree().Root.FilesDropped += OnFilesDropped;

        audioVisualsContainer.CreateBlocks();
        blockScrollBar.UpdateRange();
        audioVisualsContainer.UpdateBlocksScroll();


        MementoHandler.Instance.AddTimingMemento();
    }

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton mouseEvent:
                {
                    if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
                    {
                        // Ensure a mouse release is always captured.
                        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.MouseLeftReleased));
                        Context.Instance.IsSelectedMusicPositionMoving = false;
                    }
                    break;
                }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
                GrabFocus();
            ReleaseFocus();
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => audioVisualsContainer.UpdateNumberOfVisibleBlocks();

    private void OnFilesDropped(string[] filePaths)
    {
        if (filePaths.Length != 1)
            return;
        string path = filePaths[0];

        var audioFile = new AudioFile(path);
        Project.Instance.AudioFile = audioFile;
        MusicPlayer.LoadMp3();
    }

    private void OnScrolled(object? sender, EventArgs e)
    {
        blockScrollBar.Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }

    private void OnSeekPlaybackTime(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<float> floatArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<float>)}");
        float playbackTime = floatArgument.Value;
        MusicPlayer.SeekPlay(playbackTime);
    }
}