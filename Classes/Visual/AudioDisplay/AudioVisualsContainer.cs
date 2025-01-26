// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

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

    public int FirstTopMeasure => (int)Timing.Instance.SampleTimeToMusicPosition(0);
    public int LastTopMeasure => Timing.Instance.GetLastMeasure() - (Settings.Instance.NumberOfRows - 1);

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
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl) && !Input.IsKeyPressed(Key.Alt))
            {
                int distanceToLast = LastTopMeasure - NominalMusicPositionStartForTopBlock;
                NominalMusicPositionStartForTopBlock += Math.Min(Input.IsKeyPressed(Key.Shift) ? 5 : 1, distanceToLast);
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.AudioVisualsContainerScrolled));
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl) && !Input.IsKeyPressed(Key.Alt))
            {
                int distanceToFirst = NominalMusicPositionStartForTopBlock - FirstTopMeasure;
                NominalMusicPositionStartForTopBlock -= Math.Min(Input.IsKeyPressed(Key.Shift) ? 5 : 1, distanceToFirst);
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.AudioVisualsContainerScrolled));
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                TimingPointSelection.Instance.DeselectAll();
            }
        }
    }

    public override void _Ready()
    {
        MouseExited += OnMouseExited;
        MusicPlayer.Paused += OnMusicPaused;
        GlobalEvents.Instance.TimingPointAdded += OnTimingPointAdded;
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
        double sampleTime = Project.Instance.AudioFile.PlaybackTimeToSampleTime((float)playbackTime);
        double musicPosition = Timing.Instance.SampleTimeToMusicPosition((float)sampleTime);
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
            waveformWindow.Visible = i < Settings.Instance.NumberOfRows;
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
        if (e is not GlobalEvents.ObjectArgument<double> doubleArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<double>)}");
        double playbackTime = doubleArgument.Value;
        SeekPlaybackTime?.Invoke(this, new GlobalEvents.ObjectArgument<double>(playbackTime));
    }

    private void OnAttemptToAddTimingPoint(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<double> doubleArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<double>)}");
        double time = doubleArgument.Value;

        Timing.Instance.AddTimingPoint(time, out TimingPoint? timingPoint);
        if (timingPoint == null)
            return;
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"{nameof(timingPoint.MusicPosition)} was null");

        double musicPosition = (float)timingPoint.MusicPosition;
        Timing.Instance.SnapTimingPoint(timingPoint, musicPosition);
        Context.Instance.HeldTimingPoint = timingPoint;
        Context.Instance.HeldPointIsJustBeingAdded = true;

        TimingPointSelection.Instance.SelectTimingPoint(timingPoint);

        MementoHandler.Instance.AddTimingMemento();
    }

    private void OnTimingPointAdded(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<TimingPoint>)}");

        if (!Settings.Instance.AutoScrollWhenAddingTimingPoints)
            return;

        var timingPoint = timingPointArgument.Value;
        if (timingPoint.MusicPosition == null) 
            return;
        double musicPosition = (float)timingPoint.MusicPosition!;

        SetTopBlockToPosition(musicPosition);
    }

    /// <summary>
    /// Changes the Block scroll such that the top block contains the music position. 
    /// If more than one block contains the music position due to timeline offset settings, select the last one that does.
    /// </summary>
    /// <param name="musicPosition"></param>
    private void SetTopBlockToPosition(double musicPosition)
    {
        // Get the last ActualMusicPositionStart which is smaller than musicPosition. This way, we account for Timeline overlap.
        int bestNominalMeasureStart = (int)(musicPosition - 1);
        for (int measure = bestNominalMeasureStart; measure <= bestNominalMeasureStart + 2; measure++)
        {
            double actualStartForThisMeasure = AudioDisplayPanel.ActualMusicPositionStart(measure);
            if (actualStartForThisMeasure > musicPosition)
            {
                NominalMusicPositionStartForTopBlock = bestNominalMeasureStart;
                break;
            }
            bestNominalMeasureStart = measure;
        }
    }
}