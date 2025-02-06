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
using Godot;
using GodotPlugins.Game;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Visual;

/// <summary>
///     Parent class for window containing waveform(s), playhead and timing grid
/// </summary>
public partial class AudioDisplayPanel : Control
{
    #region Properties & Signals
    public event EventHandler SeekPlaybackTime = null!;

    public event EventHandler AttemptToAddTimingPoint = null!;

    [Export]
    public Line2D Playhead = null!;
    [Export]
    private Node2D waveformSegments = null!;
    [Export]
    private Node2D VisualTimingPointFolder = null!;
    [Export]
    private Node2D GridFolder = null!;
    [Export]
    private PreviewLine PreviewLine = null!;
    [Export]
    private Line2D SelectedPositionLine = null!;
    [Export]
    private ColorRect? VisualSelector;
    //[Export]
    //private Label MeasureLabel;
    //[Export]
    //private TimeSignatureLineEdit TimeSignatureLineEdit;

    [Export]
    private PackedScene packedVisualTimingPoint = null!;

    public bool IsInstantiating = true;

    private bool mouseIsInside = false;

    private int measurePositionStart;

    public int NominalMeasurePosition
    {
        get => measurePositionStart;
        set
        {
            if (measurePositionStart == value)
                return;
            measurePositionStart = value;

            UpdateVisuals();
        }
    }

    /// <summary>
    /// For any <see cref="AudioDisplayPanel"/>, returns the music position where this panel actually starts with the current settings.
    /// </summary>
    /// <param name="nominalMeasurePositionStart"></param>
    /// <returns></returns>
    public static float ActualMeasurePositionStart(int nominalMeasurePositionStart) 
        => nominalMeasurePositionStart - Settings.Instance.MeasureOverlap - Settings.Instance.DownbeatPositionOffset;

    public float ActualMeasurePositionStartForPanel
    {
        get => ActualMeasurePositionStart(NominalMeasurePosition);
        private set { }
    }

    public float ActualMeasurePositionEndForPanel
    {
        get => ActualMeasurePositionStartForPanel + 1 + (2 * Settings.Instance.MeasureOverlap);
        private set { }
    }

    /// <summary>
    ///     List of horizontally stacked waveforms to display
    /// </summary>
    //public List<Waveform> Waveforms = new List<Waveform>();

    //public float StartTime = 0;
    //public float EndTime = 10;
    public int FirstTimingPointIndex;

    public int LastTimingPointIndex;

    public override string ToString() => "AudioDisplayPanel";

    #endregion

    #region Godot & Signals

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Playhead.ZIndex = 100;

        UpdatePlayheadScaling();
        UpdatePreviewLineScaling();
        UpdateSelectedPositionScaling();
        UpdateSelectedPositionLine();

        CreateEmptyVisualTimingPoints(8);

        Resized += OnResized;
        GlobalEvents.Instance.SettingsChanged += OnSettingsChanged;
        GlobalEvents.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
        GlobalEvents.Instance.AudioVisualsContainerScrolled += OnScrolled;
        GlobalEvents.Instance.AudioFileChanged += OnAudioFileChanged;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        GlobalEvents.Instance.TimingChanged += OnTimingChanged;

        TimingPointSelection.Instance.SelectorChanged += OnSelectorChanged;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouse mouseEvent)
        {
            //GD.Print("AudioDisplayPanel._GuiInput(): Input was not a mouse event");
            return;
        }

        //GD.Print($"Node handling this mouse input: {this}");

        Vector2 mousePos = mouseEvent.Position;
        float measurePosition = GetMouseMeasurePosition(mousePos);
        float sampletime = Timing.Instance.MeasurePositionToSampleTime(measurePosition);

        TimingPoint? nearestTimingPoint = Timing.Instance.GetNearestTimingPoint(measurePosition);
        Context.Instance.TimingPointNearestCursor = nearestTimingPoint;
        float offsetPerWheelScroll = Input.IsKeyPressed(Key.Shift) ? 0.01f : 0.002f;

        switch (mouseEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButtonEvent 
            when !Input.IsKeyPressed(Key.Alt) && !Context.Instance.AreAnySubwindowsVisible:
                    AttemptToAddTimingPoint?.Invoke(this, new GlobalEvents.ObjectArgument<float>(sampletime));
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, DoubleClick: true } mouseButtonEvent 
            when Input.IsKeyPressed(Key.Alt):
                TimingPointSelection.Instance.DeselectAll();
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButtonEvent 
            when Input.IsKeyPressed(Key.Alt):
                TimingPointSelection.Instance.StartSelector(measurePosition);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } mouseButtonEvent 
            when !Input.IsKeyPressed(Key.Alt):
                var heldTimingPoint = Context.Instance?.HeldTimingPoint;
                var seekTime = heldTimingPoint != null ? heldTimingPoint.Offset - 0.05f : sampletime;
                SeekPlaybackTime?.Invoke(this, new GlobalEvents.ObjectArgument<float>(seekTime));
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } mouseButtonEvent 
            when Input.IsKeyPressed(Key.Ctrl) && !Input.IsKeyPressed(Key.Alt):
                TimingPointSelection.Instance.OffsetSelectionOrPoint(nearestTimingPoint, offsetPerWheelScroll);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } mouseButtonEvent 
            when Input.IsKeyPressed(Key.Ctrl) && !Input.IsKeyPressed(Key.Alt):
                TimingPointSelection.Instance.OffsetSelectionOrPoint(nearestTimingPoint, -offsetPerWheelScroll);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } mouseButtonEvent 
            when Input.IsKeyPressed(Key.Alt):
                if (nearestTimingPoint == null) break;
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBpm = nearestTimingPoint.Bpm;
                float newBpm = (int)previousBpm - 1;
                if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Ctrl))
                    newBpm = (int)previousBpm - 5;
                else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Ctrl))
                    newBpm = previousBpm - 0.1f;

                nearestTimingPoint.Bpm = newBpm;

                MementoHandler.Instance.AddTimingMemento(nearestTimingPoint);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } mouseButtonEvent 
            when Input.IsKeyPressed(Key.Alt):
                if (nearestTimingPoint == null) break;
                // Increase BPM by 1 (snapping to integers) - only for last timing point.
                previousBpm = nearestTimingPoint.Bpm;
                newBpm = (int)previousBpm + 1;
                if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Ctrl))
                    newBpm = (int)previousBpm + 5;
                else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Ctrl))
                    newBpm = previousBpm + 0.1f;

                nearestTimingPoint.Bpm = newBpm;

                MementoHandler.Instance.AddTimingMemento(nearestTimingPoint);
                break;

            default:
                return;
        }
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// Gets the raw or snapped mouse music position
    /// </summary>
    /// <param name="mouseEvent"></param>
    /// <returns></returns>
    private float GetMouseMeasurePosition(Vector2 mousePositionInLocalCoords)
    {
        float measurePosition = XPositionToMeasurePosition(mousePositionInLocalCoords.X);
        if (Input.IsKeyPressed(Key.Shift))
            measurePosition = Timing.Instance.SnapMeasurePosition(measurePosition);

        return measurePosition;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        switch (@event)
        {
            case InputEventKey { Keycode: Key.Shift }:

                {
                    //if (!mouseIsInside) return;
                    ////var mousePos = GetViewport().GetMousePosition();
                    //var mousePos = GetLocalMousePosition();
                    //float measurePosition = XPositionToMeasurePosition(mousePos.X);
                    //GD.Print("First measurePosition = " + measurePosition);
                    //if (keyEvent.Pressed)
                    //{
                    //    measurePosition = Timing.SnapMeasurePosition(measurePosition);
                    //}
                    //GD.Print("Second measurePosition = " + measurePosition);
                    //UpdatePreviewLinePosition(measurePosition);
                    //GetViewport().SetInputAsHandled();

                    UpdatePreviewLinePosition();
                    break;
                }
            case InputEventMouseMotion mouseMotion:
                // Godot's _GuiInput won't seem to let other Nodes handle the event if the mouse has been held down while moving from this Control to another
                // Therefore mouse input is handled in _Input
                if (!mouseIsInside)
                    return; 

                Vector2 globalPosition = GetGlobalRect().Position;
                Vector2 mousePos = mouseMotion.Position - globalPosition;
                float measurePosition = GetMouseMeasurePosition(mousePos);

                UpdatePreviewLinePosition();

                if (Input.IsKeyPressed(Key.Alt))
                    TimingPointSelection.Instance.UpdateSelector(measurePosition);

                // From here on down: update held timing point
                if (Context.Instance.HeldTimingPoint == null)
                    return;

                if (Input.IsKeyPressed(Key.Ctrl))
                {
                    float xMovement = mouseMotion.Relative.X;
                    float secondsPerPixel = 0.0002f;
                    float secondsDifference = xMovement * secondsPerPixel;
                    //Context.Instance.HeldTimingPoint.Offset_Set(Context.Instance.HeldTimingPoint.Offset - secondsDifference, Timing.Instance);
                    TimingPointSelection.Instance.OffsetSelection(-secondsDifference);
                    return;
                }
                else if (Input.IsKeyPressed(Key.Shift))
                {
                    float xMovement = mouseMotion.Relative.X;
                    float bpmPerPixel = 0.02f;
                    float bpmDifference = xMovement * bpmPerPixel;
                    Context.Instance.HeldTimingPoint.Bpm = Context.Instance.HeldTimingPoint.Bpm + bpmDifference;
                    return;
                }

                //Timing.Instance.SnapTimingPoint(Context.Instance.HeldTimingPoint, measurePosition, out _);
                TimingPointSelection.Instance.MoveSelection(Context.Instance.HeldTimingPoint.MeasurePosition, measurePosition);

                GetViewport().SetInputAsHandled();

                break;

        }
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        if (!Visible || Timing.Instance.IsInstantiating || Timing.Instance.IsBatchOperationInProgress)
            return;
        UpdateTimingPointsIndices();
        CreateWaveforms();
        RenderVisualTimingPoints();
        CreateGridLines();
    }

    private void OnAudioFileChanged(object? sender, EventArgs e) => UpdateVisuals();

    private void OnResized() =>
        //GD.Print("Resized!");
        UpdateVisuals();

    private void OnSettingsChanged(object? sender, EventArgs e) => UpdateVisuals();

    private void OnSelectedPositionChanged(object? sender, EventArgs e) => UpdateSelectedPositionLine();

    private void OnMouseEntered()
    {
        PreviewLine.Visible = true;
        mouseIsInside = true;
    }

    private void OnMouseExited()
    {
        PreviewLine.Visible = false;
        mouseIsInside = false;
    }

    private void OnScrolled(object? sender, EventArgs e) => UpdateSelectedPositionLine();

    private void OnSelectorChanged(object? sender, EventArgs e) => UpdateVisualSelector();
    #endregion

    #region Render

    public void CreateWaveforms()
    {
        foreach (Node? child in waveformSegments.GetChildren())
        {
            if (child is Waveform waveform)
                waveform.QueueFree();
            else if (child is Sprite2D sprite)
                sprite.QueueFree();
        }

        float margin = Settings.Instance.MeasureOverlap;

        float timeWherePanelStarts = Timing.Instance.MeasurePositionToSampleTime(ActualMeasurePositionStartForPanel);
        float timeWherePanelEnds = Timing.Instance.MeasurePositionToSampleTime(ActualMeasurePositionEndForPanel);

        TimingPoint? previousTimingPoint = Timing.Instance.GetOperatingTimingPoint_ByMeasurePosition(ActualMeasurePositionStartForPanel);

        // If the real first one exactly coincides with the start, it's ignored, which doesn't matter
        TimingPoint? firstTimingPointInPanel = Timing.Instance.GetNextTimingPoint(previousTimingPoint); 
        firstTimingPointInPanel = firstTimingPointInPanel?.MeasurePosition > ActualMeasurePositionEndForPanel ? null : firstTimingPointInPanel;

        // Create first waveform segment
        AddWaveformSegment(timeWherePanelStarts, firstTimingPointInPanel?.Offset ?? timeWherePanelEnds);

        if (firstTimingPointInPanel == null)
            return;

        // Create a waveform segment startin on each timingpoint that is visible in this display panel
        int firstPointIndex = Timing.Instance.TimingPoints.IndexOf(firstTimingPointInPanel);
        for (int i = firstPointIndex; Timing.Instance.TimingPoints[i]?.MeasurePosition < ActualMeasurePositionEndForPanel; i++)
        {
            TimingPoint timingPoint = Timing.Instance.TimingPoints[i];

            bool isNextPointOutOfRange = (i + 1 >= Timing.Instance.TimingPoints.Count);
            bool isNextPointOutsideOfPanel = isNextPointOutOfRange
                || (Timing.Instance.TimingPoints[i + 1].MeasurePosition > ActualMeasurePositionEndForPanel);

            float waveSegmentStartTime = Timing.Instance.TimingPoints[i].Offset;
            float waveSegmentEndTime = isNextPointOutsideOfPanel ? timeWherePanelEnds : Timing.Instance.TimingPoints[i + 1].Offset;

            AddWaveformSegment(waveSegmentStartTime, waveSegmentEndTime);

            if (isNextPointOutOfRange)
                return;
        }
    }

    private void AddWaveformSegment(float waveSegmentStartTime, float waveSegmentEndTime)
    {
        float measurePositionStart = Timing.Instance.SampleTimeToMeasurePosition(waveSegmentStartTime);
        float measurePositionEnd = Timing.Instance.SampleTimeToMeasurePosition(waveSegmentEndTime);

        float margin = Settings.Instance.MeasureOverlap;
        TimingPoint? heldTimingPoint = Context.Instance.HeldTimingPoint;

        float panelLengthInMeasures = (1f + (2 * margin));
        float length = Size.X * (measurePositionEnd - measurePositionStart) / panelLengthInMeasures;

        float relativePositionStart = (measurePositionStart - ActualMeasurePositionStartForPanel);
        float xPosition = Size.X * relativePositionStart / panelLengthInMeasures;

        bool canHeldTimingPointBeInSegment = (heldTimingPoint == null)
            || (
                Timing.Instance.CanTimingPointGoHere(heldTimingPoint, measurePositionStart, out var _)
                || Timing.Instance.CanTimingPointGoHere(heldTimingPoint, measurePositionEnd, out var _)
                )
            || (Time.GetTicksMsec() - heldTimingPoint.SystemTimeWhenCreatedMsec) < 30; ;

        var waveform = new Waveform(Project.Instance.AudioFile, length, Size.Y, [waveSegmentStartTime, waveSegmentEndTime])
        {
            Position = new Vector2(xPosition, Size.Y / 2),
            Color = (canHeldTimingPointBeInSegment ? Waveform.defaultColor : Waveform.darkenedColor)
        };

        waveformSegments.AddChild(waveform);
    }

    /// <summary>
    ///     Instantiate an amount of <see cref="VisualTimingPoint" /> nodes and adds as children.
    ///     These are then modified whenever the visuals need to change, instead of re-instantiating them.
    ///     This is significantly faster than creating them anew each time.
    /// </summary>
    public void CreateEmptyVisualTimingPoints(int amount)
    {
        foreach (Node? child in VisualTimingPointFolder.GetChildren())
        {
            if (child is VisualTimingPoint visualTimingPoint)
                visualTimingPoint.QueueFree();
        }

        var dummyTimingPoint = new TimingPoint(0f, 0f, 120);

        for (int i = 0; i < amount; i++)
        {
            AddVisualTimingPoint(dummyTimingPoint, out _);
        }
    }

    public void AddVisualTimingPoint(TimingPoint timingPoint, out VisualTimingPoint visualTimingPoint)
    {
        if (packedVisualTimingPoint == null)
            throw new NullReferenceException($"No scene loaded for {nameof(packedVisualTimingPoint)}");
        Node instantiatedVisualTimingPoint = packedVisualTimingPoint.Instantiate() ?? throw new NullReferenceException($"{nameof(packedVisualTimingPoint)} instantiated as null");
        visualTimingPoint = (VisualTimingPoint)instantiatedVisualTimingPoint;

        visualTimingPoint.Visible = false;
        //visualTimingPoint.Scale = new Vector2(0.2f, 0.2f);
        visualTimingPoint.ZIndex = 95;
        visualTimingPoint.TimingPoint = timingPoint;
        VisualTimingPointFolder.AddChild(visualTimingPoint);
    }

    public void RenderVisualTimingPoints()
    {
        Godot.Collections.Array<Node> children = VisualTimingPointFolder.GetChildren();
        int childrenAmount = children.Count;
        int index = 0;

        foreach (Control child in children.Cast<Control>())
            child.Visible = false;

        // this assumes all children are VisualTimingPoint
        foreach (TimingPoint timingPoint in Timing.Instance.TimingPoints)
        {
            if (timingPoint == null)
                throw new NullReferenceException($"{nameof(timingPoint)} was null");
            if (timingPoint.MeasurePosition == null)
                throw new NullReferenceException($"{nameof(timingPoint.MeasurePosition)} was null");

            if (timingPoint.MeasurePosition < ActualMeasurePositionStartForPanel
                || timingPoint.MeasurePosition >= ActualMeasurePositionEndForPanel)
            {
                continue;
            }

            VisualTimingPoint visualTimingPoint;
            if (index >= childrenAmount)
            {
                AddVisualTimingPoint(timingPoint, out visualTimingPoint);
            }
            else
            {
                Node child = children[index];
                if (child is not VisualTimingPoint or null)
                    continue;
                else
                    visualTimingPoint = (VisualTimingPoint)child;

                if (visualTimingPoint == null)
                    throw new NullReferenceException($"{nameof(VisualTimingPoint)} was null");
            }
            float x = MeasurePositionToXPosition((float)timingPoint.MeasurePosition);
            visualTimingPoint.TimingPoint = timingPoint;
            visualTimingPoint.Position = new Vector2(x, Size.Y / 2);
            visualTimingPoint.GrabArea.Size = new Vector2(visualTimingPoint.GrabWidth, Size.Y);
            visualTimingPoint.GrabArea.Position = new Vector2(-visualTimingPoint.GrabWidth / 2, -Size.Y / 2);
            visualTimingPoint.BpmLabel.Position = new Vector2(0, -Size.Y / 2);
            visualTimingPoint.BpmLabel.Position = new Vector2(0, -Size.Y / 2); // This is dumb, but it's the easiest way to fix Position being wrong when scrolling.
            visualTimingPoint.OffsetLine.Points = [
                new(0, -Size.Y * 1 / 4f),
                new(0, Size.Y * 1 / 4f)
            ];
            visualTimingPoint.LineDefaultHeight = Size.Y * 1 / 2f;
            visualTimingPoint.UpdateLabels(timingPoint);
            visualTimingPoint.Visible = true;
            index++;
        }
    }

    public void CreateGridLines()
    {
        foreach (Node? child in GridFolder.GetChildren())
            child.QueueFree();

        int divisor = Settings.Instance.GridDivisor;
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMeasurePosition - 1);

        int measureOffset = -1;
        int divisionIndex = 0;
        while (divisionIndex < 50)
        {
            //using (
            GridLine gridLine = GetGridLine(timeSignature, divisor, divisionIndex, measureOffset); //)
                                                                                                   //{
            divisionIndex++;

            float measurePosition = gridLine.RelativeMeasurePosition + NominalMeasurePosition + measureOffset;

            if (measurePosition >= NominalMeasurePosition && measureOffset == -1)
            {
                divisionIndex = 0;
                measureOffset += 1;
                timeSignature = Timing.Instance.GetTimeSignature(NominalMeasurePosition + measureOffset);
                continue;
            }

            if (measurePosition >= NominalMeasurePosition + 1 && measureOffset == 0)
            {
                divisionIndex = 0;
                measureOffset += 1;
                timeSignature = Timing.Instance.GetTimeSignature(NominalMeasurePosition + measureOffset);
                continue;
            }

            if (measurePosition < ActualMeasurePositionStartForPanel)
                continue;
            if (measurePosition > ActualMeasurePositionStartForPanel + 1 + (2 * Settings.Instance.MeasureOverlap))
                break;

            timeSignature = Timing.Instance.GetTimeSignature(measurePosition);

            gridLine.ZIndex = 0;

            GridFolder.AddChild(gridLine);
            //}
        }
    }

    public GridLine GetGridLine(int[] timeSignature, int divisor, int index, int measureOffset)
    {
        var gridLine = new GridLine(timeSignature, divisor, index, Size.Y);

        float offset = Settings.Instance.DownbeatPositionOffset;
        float margin = Settings.Instance.MeasureOverlap;
        float xPosition = Size.X * ((gridLine.RelativeMeasurePosition + measureOffset + margin + offset) / ((2 * margin) + 1f));
        gridLine.Position = new Vector2(xPosition, Size.Y/2);
        gridLine.ZIndex = 90;

        return gridLine;
    }

    #endregion

    #region Updaters

    public void UpdateVisuals()
    {
        if (IsInstantiating)
            return;
        if (!Visible)
            return;
        if (Size.X <= 0)
            return;
        UpdatePlayheadScaling();
        UpdatePreviewLineScaling();
        UpdatePreviewLinePosition();
        UpdateSelectedPositionScaling();
        CreateWaveforms(); // takes 2-5 ms on 30 blocks  loaded
        RenderVisualTimingPoints(); // takes 2-3 ms on 30 blocks loaded
        CreateGridLines();
        UpdateVisualSelector();

        // This works fine
        //await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        //GetViewport().GetTexture().GetImage().SavePng("user://renderedWave.png");
    }

    public void UpdatePlayheadScaling()
    {
        Playhead.Points = [
            new(0, 0),
            new(0, Size.Y)
        ];
    }

    public void UpdatePreviewLineScaling()
    {
        PreviewLine.Points = [
            new(0, Size.Y/4f),
            new(0, Size.Y*3/4f)
        ];
    }

    //private void UpdatePreviewLinePosition(float measurePosition)
    //{
    //    PreviewLine.Position = new Vector2(MeasurePositionToXPosition(measurePosition), 0);

    //    TimeSpan musicTime = TimeSpan.FromSeconds(Timing.Instance.MeasurePositionToTime(measurePosition));
    //    PreviewLine.TimeLabel.Text = musicTime.ToString(@"mm\:ss\:fff");
    //}

    private void UpdatePreviewLinePosition()
    {
        Vector2 mousePos = GetLocalMousePosition();
        float measurePosition = XPositionToMeasurePosition(mousePos.X);

        if (Input.IsKeyPressed(Key.Shift))
        {
            measurePosition = Timing.Instance.SnapMeasurePosition(measurePosition);
        }

        PreviewLine.Position = new Vector2(MeasurePositionToXPosition(measurePosition), 0);

        float time = Timing.Instance.MeasurePositionToSampleTime(measurePosition);
        var musicTime = TimeSpan.FromSeconds(time);
        PreviewLine.TimeLabel.Text = (time < 0 ? "-" : "") + musicTime.ToString(@"mm\:ss\:fff");
    }

    public void UpdateSelectedPositionLine()
    {
        float x = MeasurePositionToXPosition(Context.Instance.SelectedMeasurePosition);
        SelectedPositionLine.Position = new Vector2(x, 0);
        //SelectedPositionLine.Visible = x >= 0 && x <= Size.X;
    }

    public void UpdateSelectedPositionScaling()
    {
        SelectedPositionLine.Points = [
            new(0, 0),
            new(0, Size.Y)
        ];
    }

    public void UpdateTimingPointsIndices()
    {
        if (Timing.Instance.TimingPoints.Count == 0)
            return;

        float margin = Settings.Instance.MeasureOverlap;

        int firstIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MeasurePosition <= ActualMeasurePositionStartForPanel);
        // If there's only TimingPoints AFTER MeasurePositionStart
        if (firstIndex == -1)
            firstIndex = Timing.Instance.TimingPoints.FindIndex(point => point.MeasurePosition > ActualMeasurePositionStartForPanel);
        int lastIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MeasurePosition < ActualMeasurePositionStartForPanel + 1 + (2 * margin));
        if (lastIndex == -1)
            lastIndex = firstIndex;

        FirstTimingPointIndex = firstIndex;
        LastTimingPointIndex = lastIndex;
    }

    private void UpdateVisualSelector()
    {
        if (VisualSelector == null)
            return;

        if (TimingPointSelection.Instance.SelectorStartPosition == null || TimingPointSelection.Instance.SelectorEndPosition == null)
        {
            VisualSelector.Visible = false;
            return;
        }
        float xAtSelectorStart = MeasurePositionToXPosition((float)TimingPointSelection.Instance.SelectorStartPosition);
        float xAtSelectorEnd = MeasurePositionToXPosition((float)TimingPointSelection.Instance.SelectorEndPosition);
        if (xAtSelectorStart > Size.X || xAtSelectorEnd < 0)
        {
            VisualSelector.Visible = false;
            return;
        }

        xAtSelectorStart = Math.Max(xAtSelectorStart, 0);
        xAtSelectorEnd = Math.Min(xAtSelectorEnd, Size.X);

        VisualSelector.Visible = true;

        VisualSelector.Position = new Vector2(xAtSelectorStart, 0);
        VisualSelector.Size = new Vector2(xAtSelectorEnd - xAtSelectorStart, Size.Y);
    }

    #endregion

    #region Calculators

    public float XPositionToMeasurePosition(float x) =>
        //GD.Print($"XPositionToMeasurePosition: NominalMeasurePositionStartForWindow = {NominalMeasurePositionStartForWindow}");
        XPositionToRelativeMeasurePosition(x) + NominalMeasurePosition;

    /// <summary>
    ///     Return music position relative to <see cref="NominalMeasurePosition" />
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public float XPositionToRelativeMeasurePosition(float x)
    {
        float offset = Settings.Instance.DownbeatPositionOffset;
        float margin = Settings.Instance.MeasureOverlap;
        float windowLengthInMeasures = 1f + (2 * margin);
        return (x * windowLengthInMeasures / Size.X) - margin - offset;
    }

    public float MeasurePositionToXPosition(float measurePosition)
    {
        float offset = Settings.Instance.DownbeatPositionOffset;
        float margin = Settings.Instance.MeasureOverlap;
        float windowLengthInMeasures = 1f + (2 * margin);
        return Size.X * (measurePosition - NominalMeasurePosition + margin + offset) / windowLengthInMeasures;
    }

    #endregion
}