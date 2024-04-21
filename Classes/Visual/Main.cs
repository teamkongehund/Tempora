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
    [Export]
    private MusicPlayer musicPlayer = null!;
    [Export]
    private AudioVisualsContainer audioVisualsContainer = null!;
    [Export]
    private Metronome metronome = null!;
    [Export]
    private BlockScrollBar blockScrollBar = null!;
    [Export]
    private HScrollBar blockAmountScrollBar = null!;
    [Export]
    private HScrollBar gridScrollBar = null!;
    [Export]
    private HScrollBar offsetScrollBar = null!;
    [Export]
    private HScrollBar overlapScrollBar = null!;
    [Export]
    private HScrollBar playbackRateScrollBar = null!;
    [Export]
    private Label errorLabel = null!;

    //AudioFile AudioFile;

    private ProjectFileManager projectFileManager = null!;

    //private AudioDisplayPanel waveformWindow;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        projectFileManager = ProjectFileManager.Instance;

        // This works in Debug if we use i.e. audioPath = "res://Audio/UMO.mp3",
        // but won't work in production, as resources are converted to different file formats.
        //Project.Instance.AudioFile = new AudioFile(audioPath);

        Project.Instance.AudioFile = new AudioFile(defaultMP3);

        Signals.Instance.Scrolled += OnScrolled;
        Signals.Instance.SettingsChanged += OnSettingsChanged;
        audioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
        GetTree().Root.FilesDropped += OnFilesDropped;

        audioVisualsContainer.CreateBlocks();
        blockScrollBar.UpdateRange();
        audioVisualsContainer.UpdateBlocksScroll();

        ActionsHandler.Instance.AddTimingMemento();
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
                        Signals.Instance.EmitEvent(Signals.Events.MouseLeftReleased);
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
        musicPlayer.LoadMp3();
    }

    private void OnScrolled(object? sender, EventArgs e)
    {
        blockScrollBar.Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }

    private void OnSeekPlaybackTime(object? sender, EventArgs e)
    {
        if (e is not Signals.ObjectArgument<float> floatArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(Signals.ObjectArgument<float>)}");
        float playbackTime = floatArgument.Value;
        musicPlayer.SeekPlay(playbackTime);
    }
}