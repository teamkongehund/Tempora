using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Visual;

public partial class AudioVisualsContainer : VBoxContainer
{
    //[Export] public int NumberOfBlocks = 10;

    /// <summary>
    ///     Forward childrens' signals to Main
    /// </summary>
    public event EventHandler SeekPlaybackTime = null!;

    [Export]
    private PackedScene packedAudioBlock = null!;

    private MusicPlayer MusicPlayer => MusicPlayer.Instance;

    //public float FirstBlockStartTime = 0;

    private int nominalMusicPositionStartForTopBlock;

    public List<AudioBlock> AudioBlocks = [];

    public int NominalMusicPositionStartForTopBlock
    {
        get => nominalMusicPositionStartForTopBlock;
        set
        {
            if (value == nominalMusicPositionStartForTopBlock)
                return;
            nominalMusicPositionStartForTopBlock = value;
            UpdateBlocksScroll();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl))
            {
                NominalMusicPositionStartForTopBlock += Input.IsKeyPressed(Key.Shift) ? 5 : 1;
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Scrolled));
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl))
            {
                NominalMusicPositionStartForTopBlock -= Input.IsKeyPressed(Key.Shift) ? 5 : 1;
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Scrolled));
            }
        }
    }

    public override void _Ready()
    {
        MouseExited += OnMouseExited;
        MusicPlayer.Paused += OnMusicPaused;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (MusicPlayer.Playing)
        {
            UpdatePlayHeads();
        }
    }

    private void UpdatePlayHeads()
    {
        double playbackTime = MusicPlayer.GetPlaybackTime();
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
        foreach (AudioBlock audioBlock in GetChildren().OfType<AudioBlock>())
        {
            AudioDisplayPanel audioDisplayPanel = audioBlock.AudioDisplayPanel;
            float x = audioDisplayPanel.MusicPositionToXPosition(musicPosition);
            audioDisplayPanel.Playhead.Position = new Vector2(x, 0.0f);
            audioDisplayPanel.Playhead.Visible = x >= 0 && x <= audioDisplayPanel.Size.X && MusicPlayer.Playing;
        }
    }

    private void OnMouseExited()
    {
        Context.Instance.TimingPointNearestCursor = null;
    }

    private void OnMusicPaused() => UpdatePlayHeads();

    public void CreateBlocks()
    {
        foreach (Node? child in GetChildren())
            child.QueueFree();
        AudioBlocks.Clear();

        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        // Instantiate block scenes and add as children
        for (int i = 0; i < Settings.Instance.MaxNumberOfBlocks; i++)
        {
            if (packedAudioBlock.Instantiate() is not AudioBlock audioBlock)
            {
                throw new NullReferenceException(nameof(audioBlock));
            }
            audioBlock.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;
            AddChild(audioBlock);
            AudioBlocks.Add(audioBlock);
        }

        foreach (AudioBlock audioBlock in AudioBlocks)
        {
            AudioDisplayPanel audioDisplayPanel = audioBlock.AudioDisplayPanel;
            audioDisplayPanel.SizeFlagsVertical = SizeFlags.ExpandFill;

            audioDisplayPanel.Playhead.Visible = false;

            audioDisplayPanel.SeekPlaybackTime += OnSeekPlaybackTime;
            audioDisplayPanel.AttemptToAddTimingPoint += OnAttemptToAddTimingPoint;

            audioDisplayPanel.IsInstantiating = false;
        }

        UpdateNumberOfVisibleBlocks();
    }

    public void UpdateNumberOfVisibleBlocks()
    {
        Godot.Collections.Array<Node> children = GetChildren();

        for (int i = 0; i < children.Count; i++)
        {
            var waveformWindow = (AudioBlock)children[i];
            waveformWindow.Visible = i < Settings.Instance.NumberOfBlocks;
        }
    }

    public void UpdateBlocksScroll()
    {
        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        Godot.Collections.Array<Node> children = GetChildren();

        foreach (Node? child in children)
        {
            if (child is not AudioBlock)
            {
                continue;
            }
            var audioBlock = (AudioBlock)child;
            audioBlock.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;
        }
    }

    private void OnSeekPlaybackTime(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<float> floatArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<float>)}");
        float playbackTime = floatArgument.Value;
        SeekPlaybackTime?.Invoke(this, new GlobalEvents.ObjectArgument<float>(playbackTime));
    }

    private void OnAttemptToAddTimingPoint(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<float> floatArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<float>)}");
        float time = floatArgument.Value;

        Timing.Instance.AddTimingPoint(time, out TimingPoint? timingPoint);
        if (timingPoint == null)
            return;
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"{nameof(timingPoint.MusicPosition)} was null");

        float musicPosition = (float)timingPoint.MusicPosition;
        Timing.Instance.SnapTimingPoint(timingPoint, musicPosition, out bool didSnapSucceed);
        Context.Instance.HeldTimingPoint = timingPoint;

        if (!didSnapSucceed)
        {
            Context.Instance.HeldTimingPoint = null;
            Timing.Instance.TimingPoints.Remove(timingPoint);
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged)); // Gets rid of VisualTimingPoint
            return;
        }

        MementoHandler.Instance.AddTimingMemento();
    }
}