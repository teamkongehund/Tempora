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

namespace Tempora.Classes.Visual.AudioDisplay;

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

    private int nominalMeasurePositionStartForTopBlock;

    public List<AudioBlock> AudioBlocks = [];

    public int FirstTopMeasure => Timing.Instance.TimeSignaturePoints.Count > 0
            ? Math.Min((int)Timing.Instance.OffsetToMeasurePosition(0), Timing.Instance.TimeSignaturePoints[0].Measure)
            : (int)Timing.Instance.OffsetToMeasurePosition(0);
    public int LastTopMeasure => Timing.Instance.GetLastMeasure() - (Settings.Instance.NumberOfRows - 1);

    public int NominalMeasurePositionStartForTopBlock
    {
        get => nominalMeasurePositionStartForTopBlock;
        set
        {
            if (value == nominalMeasurePositionStartForTopBlock)
                return;
            nominalMeasurePositionStartForTopBlock = value;
            UpdateBlocksScroll();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl) && !Input.IsKeyPressed(Key.Alt))
            {
                int distanceToLast = LastTopMeasure - NominalMeasurePositionStartForTopBlock;
                NominalMeasurePositionStartForTopBlock += Math.Min(Input.IsKeyPressed(Key.Shift) ? 5 : 1, distanceToLast);
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.AudioVisualsContainerScrolled));
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl) && !Input.IsKeyPressed(Key.Alt))
            {
                int distanceToFirst = NominalMeasurePositionStartForTopBlock - FirstTopMeasure;
                NominalMeasurePositionStartForTopBlock -= Math.Min(Input.IsKeyPressed(Key.Shift) ? 5 : 1, distanceToFirst);
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
        MusicPlayer.Finished += OnMusicFinished;
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
        float sampleTime = Project.Instance.AudioFile.PlaybackTimeToSampleTime((float)playbackTime);
        float measurePosition = Timing.Instance.OffsetToMeasurePosition((float)sampleTime);
        foreach (AudioBlock audioBlock in GetChildren().OfType<AudioBlock>())
        {
            AudioDisplayPanel audioDisplayPanel = audioBlock.AudioDisplayPanel;
            float x = audioDisplayPanel.MeasurePositionToXPosition(measurePosition);
            audioDisplayPanel.Playhead.Position = new Vector2(x, 0.0f);
            audioDisplayPanel.Playhead.Visible = x >= 0 && x <= audioDisplayPanel.Size.X && MusicPlayer.Playing;
        }
    }

    private void OnMouseExited()
    {
        Context.Instance.TimingPointNearestCursor = null;
    }

    private void OnMusicPaused() => UpdatePlayHeads();

    private void OnMusicFinished() => UpdatePlayHeads();

    public void CreateBlocks()
    {
        foreach (Node? child in GetChildren())
            child.QueueFree();
        AudioBlocks.Clear();

        int measurePositionStart = NominalMeasurePositionStartForTopBlock;

        // Instantiate block scenes and add as children
        for (int i = 0; i < Settings.Instance.MaxNumberOfBlocks; i++)
        {
            if (packedAudioBlock.Instantiate() is not AudioBlock audioBlock)
            {
                throw new NullReferenceException(nameof(audioBlock));
            }
            audioBlock.NominalMeasurePosition = measurePositionStart;
            measurePositionStart++;
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
        int measurePositionStart = NominalMeasurePositionStartForTopBlock;

        Godot.Collections.Array<Node> children = GetChildren();

        foreach (Node? child in children)
        {
            if (child is not AudioBlock)
            {
                continue;
            }
            var audioBlock = (AudioBlock)child;
            audioBlock.NominalMeasurePosition = measurePositionStart;
            measurePositionStart++;
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
        if (timingPoint.MeasurePosition == null)
            throw new NullReferenceException($"{nameof(timingPoint.MeasurePosition)} was null");

        float measurePosition = (float)timingPoint.MeasurePosition;
        Timing.Instance.SnapTimingPoint(timingPoint, measurePosition);
        Context.Instance.HeldTimingPoint = timingPoint;
        Context.Instance.HeldPointIsJustBeingAdded = true;

        TimingPointSelection.Instance.SelectTimingPoint(timingPoint);

        //MementoHandler.Instance.AddTimingMemento();
    }

    private void OnTimingPointAdded(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<TimingPoint>)}");

        if (!Settings.Instance.AutoScrollWhenAddingTimingPoints)
            return;

        var timingPoint = timingPointArgument.Value;
        if (timingPoint.MeasurePosition == null) 
            return;
        float measurePosition = (float)timingPoint.MeasurePosition!;

        SetTopBlockToPosition(measurePosition);
    }

    /// <summary>
    /// Changes the Block scroll such that the top block contains the music position. 
    /// If more than one block contains the music position due to timeline offset settings, select the last one that does.
    /// </summary>
    /// <param name="measurePosition"></param>
    private void SetTopBlockToPosition(float measurePosition)
    {
        // Get the last ActualMeasurePositionStart which is smaller than measurePosition. This way, we account for Timeline overlap.
        int bestNominalMeasureStart = (int)(measurePosition - 1);
        for (int measure = bestNominalMeasureStart; measure <= bestNominalMeasureStart + 2; measure++)
        {
            float actualStartForThisMeasure = AudioDisplayPanel.ActualMeasurePositionStart(measure);
            if (actualStartForThisMeasure > measurePosition)
            {
                NominalMeasurePositionStartForTopBlock = bestNominalMeasureStart;
                break;
            }
            bestNominalMeasureStart = measure;
        }
    }
}