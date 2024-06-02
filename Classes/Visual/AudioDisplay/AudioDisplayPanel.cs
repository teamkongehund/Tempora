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

    private int musicPositionStart;

    public int NominalMusicPositionStartForWindow
    {
        get => musicPositionStart;
        set
        {
            if (musicPositionStart == value)
                return;
            musicPositionStart = value;

            UpdateVisuals();
        }
    }

    /// <summary>
    /// For any <see cref="AudioDisplayPanel"/>, returns the music position where this panel actually starts with the current settings.
    /// </summary>
    /// <param name="nominalMusicPositionStart"></param>
    /// <returns></returns>
    public static float ActualMusicPositionStart(int nominalMusicPositionStart) 
        => nominalMusicPositionStart - Settings.Instance.MusicPositionMargin - Settings.Instance.MusicPositionOffset;

    public float ActualMusicPositionStartForPanel
    {
        get => ActualMusicPositionStart(NominalMusicPositionStartForWindow);
        private set { }
    }

    public float ActualMusicPositionEndForPanel
    {
        get => ActualMusicPositionStartForPanel + 1 + (2 * Settings.Instance.MusicPositionMargin);
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
        GlobalEvents.Instance.Scrolled += OnScrolled;
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
        float musicPosition = GetMouseMusicPosition(mousePos);
        float time = Timing.Instance.MusicPositionToTime(musicPosition);

        TimingPoint? nearestTimingPoint = Timing.Instance.GetNearestTimingPoint(musicPosition);
        Context.Instance.TimingPointNearestCursor = nearestTimingPoint;
        float offsetPerWheelScroll = 0.002f;

        switch (mouseEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButtonEvent when !Input.IsKeyPressed(Key.Alt):
                if (!Context.Instance.AreAnySubwindowsVisible)
                    AttemptToAddTimingPoint?.Invoke(this, new GlobalEvents.ObjectArgument<float>(time));
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, DoubleClick: true } mouseButtonEvent when Input.IsKeyPressed(Key.Alt):
                TimingPointSelection.Instance.DeselectAll();
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButtonEvent when Input.IsKeyPressed(Key.Alt):
                TimingPointSelection.Instance.StartSelector(musicPosition);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } mouseButtonEvent when !Input.IsKeyPressed(Key.Alt):
                SeekPlaybackTime?.Invoke(this, new GlobalEvents.ObjectArgument<float>(time));
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } mouseButtonEvent when Input.IsKeyPressed(Key.Ctrl):
                TimingPointSelection.Instance.OffsetSelectionOrPoint(nearestTimingPoint, offsetPerWheelScroll);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } mouseButtonEvent when Input.IsKeyPressed(Key.Ctrl):
                TimingPointSelection.Instance.OffsetSelectionOrPoint(nearestTimingPoint, -offsetPerWheelScroll);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } mouseButtonEvent when Input.IsKeyPressed(Key.Alt):
                if (nearestTimingPoint == null) break;
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBpm = nearestTimingPoint.Bpm;
                float newBpm = (int)previousBpm - 1;
                if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                    newBpm = (int)previousBpm - 5;
                else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                    newBpm = previousBpm - 0.1f;

                nearestTimingPoint.Bpm_Set(newBpm, Timing.Instance);

                MementoHandler.Instance.AddTimingMemento(nearestTimingPoint);
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } mouseButtonEvent when Input.IsKeyPressed(Key.Alt):
                if (nearestTimingPoint == null) break;
                // Increase BPM by 1 (snapping to integers) - only for last timing point.
                previousBpm = nearestTimingPoint.Bpm;
                newBpm = (int)previousBpm + 1;
                if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                    newBpm = (int)previousBpm + 5;
                else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                    newBpm = previousBpm + 0.1f;

                nearestTimingPoint.Bpm_Set(newBpm, Timing.Instance);

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
    private float GetMouseMusicPosition(Vector2 mousePositionInLocalCoords)
    {
        float musicPosition = XPositionToMusicPosition(mousePositionInLocalCoords.X);
        if (Input.IsKeyPressed(Key.Shift))
            musicPosition = Timing.Instance.SnapMusicPosition(musicPosition);

        return musicPosition;
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
                    //float musicPosition = XPositionToMusicPosition(mousePos.X);
                    //GD.Print("First musicPosition = " + musicPosition);
                    //if (keyEvent.Pressed)
                    //{
                    //    musicPosition = Timing.SnapMusicPosition(musicPosition);
                    //}
                    //GD.Print("Second musicPosition = " + musicPosition);
                    //UpdatePreviewLinePosition(musicPosition);
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
                float musicPosition = GetMouseMusicPosition(mousePos);

                UpdatePreviewLinePosition();

                if (Input.IsKeyPressed(Key.Alt))
                    TimingPointSelection.Instance.UpdateSelector(musicPosition);

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

                //Timing.Instance.SnapTimingPoint(Context.Instance.HeldTimingPoint, musicPosition, out _);
                TimingPointSelection.Instance.MoveSelection(Context.Instance.HeldTimingPoint.MusicPosition, musicPosition);

                GetViewport().SetInputAsHandled();

                break;

        }
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        if (!Visible)
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

        float margin = Settings.Instance.MusicPositionMargin;

        float timeWherePanelStarts = Timing.Instance.MusicPositionToTime(ActualMusicPositionStartForPanel);
        float timeWherePanelEnds = Timing.Instance.MusicPositionToTime(ActualMusicPositionEndForPanel);

        TimingPoint? previousTimingPoint = Timing.Instance.GetOperatingTimingPoint_ByMusicPosition(ActualMusicPositionStartForPanel);

        // If the real first one exactly coincides with the start, it's ignored, which doesn't matter
        TimingPoint? firstTimingPointInPanel = Timing.Instance.GetNextTimingPoint(previousTimingPoint); 
        firstTimingPointInPanel = firstTimingPointInPanel?.MusicPosition > ActualMusicPositionEndForPanel ? null : firstTimingPointInPanel;

        // Create first waveform segment
        AddWaveformSegment(timeWherePanelStarts, firstTimingPointInPanel?.Offset ?? timeWherePanelEnds);

        if (firstTimingPointInPanel == null)
            return;

        // Create a waveform segment startin on each timingpoint that is visible in this display panel
        int firstPointIndex = Timing.Instance.TimingPoints.IndexOf(firstTimingPointInPanel);
        for (int i = firstPointIndex; Timing.Instance.TimingPoints[i]?.MusicPosition < ActualMusicPositionEndForPanel; i++)
        {
            TimingPoint timingPoint = Timing.Instance.TimingPoints[i];

            bool isNextPointOutOfRange = (i + 1 >= Timing.Instance.TimingPoints.Count);
            bool isNextPointOutsideOfPanel = isNextPointOutOfRange
                || (Timing.Instance.TimingPoints[i + 1].MusicPosition > ActualMusicPositionEndForPanel);

            float waveSegmentStartTime = Timing.Instance.TimingPoints[i].Offset;
            float waveSegmentEndTime = isNextPointOutsideOfPanel ? timeWherePanelEnds : Timing.Instance.TimingPoints[i + 1].Offset;

            AddWaveformSegment(waveSegmentStartTime, waveSegmentEndTime);

            if (isNextPointOutOfRange)
                return;
        }
    }

    private void AddWaveformSegment(float waveSegmentStartTime, float waveSegmentEndTime)
    {
        float musicPositionStart = Timing.Instance.TimeToMusicPosition(waveSegmentStartTime);
        float musicPositionEnd = Timing.Instance.TimeToMusicPosition(waveSegmentEndTime);

        float margin = Settings.Instance.MusicPositionMargin;
        TimingPoint? heldTimingPoint = Context.Instance.HeldTimingPoint;

        float panelLengthInMeasures = (1f + (2 * margin));
        float length = Size.X * (musicPositionEnd - musicPositionStart) / panelLengthInMeasures;

        float relativePositionStart = (musicPositionStart - ActualMusicPositionStartForPanel);
        float xPosition = Size.X * relativePositionStart / panelLengthInMeasures;

        bool canHeldTimingPointBeInSegment = (heldTimingPoint == null)
            || (
                Timing.Instance.CanTimingPointGoHere(heldTimingPoint, musicPositionStart, out var _)
                || Timing.Instance.CanTimingPointGoHere(heldTimingPoint, musicPositionEnd, out var _)
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
            if (timingPoint.MusicPosition == null)
                throw new NullReferenceException($"{nameof(timingPoint.MusicPosition)} was null");

            if (timingPoint.MusicPosition < ActualMusicPositionStartForPanel
                || timingPoint.MusicPosition >= ActualMusicPositionEndForPanel)
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
            float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);
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
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow - 1);

        int measureOffset = -1;
        int divisionIndex = 0;
        while (divisionIndex < 50)
        {
            //using (
            GridLine gridLine = GetGridLine(timeSignature, divisor, divisionIndex, measureOffset); //)
                                                                                                   //{
            divisionIndex++;

            float musicPosition = gridLine.RelativeMusicPosition + NominalMusicPositionStartForWindow + measureOffset;

            if (musicPosition >= NominalMusicPositionStartForWindow && measureOffset == -1)
            {
                divisionIndex = 0;
                measureOffset += 1;
                timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow + measureOffset);
                continue;
            }

            if (musicPosition >= NominalMusicPositionStartForWindow + 1 && measureOffset == 0)
            {
                divisionIndex = 0;
                measureOffset += 1;
                timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow + measureOffset);
                continue;
            }

            if (musicPosition < ActualMusicPositionStartForPanel)
                continue;
            if (musicPosition > ActualMusicPositionStartForPanel + 1 + (2 * Settings.Instance.MusicPositionMargin))
                break;

            timeSignature = Timing.Instance.GetTimeSignature(musicPosition);

            gridLine.ZIndex = 0;

            GridFolder.AddChild(gridLine);
            //}
        }
    }

    public GridLine GetGridLine(int[] timeSignature, int divisor, int index, int measureOffset)
    {
        var gridLine = new GridLine(timeSignature, divisor, index, Size.Y);

        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float xPosition = Size.X * ((gridLine.RelativeMusicPosition + measureOffset + margin + offset) / ((2 * margin) + 1f));
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

    //private void UpdatePreviewLinePosition(float musicPosition)
    //{
    //    PreviewLine.Position = new Vector2(MusicPositionToXPosition(musicPosition), 0);

    //    TimeSpan musicTime = TimeSpan.FromSeconds(Timing.Instance.MusicPositionToTime(musicPosition));
    //    PreviewLine.TimeLabel.Text = musicTime.ToString(@"mm\:ss\:fff");
    //}

    private void UpdatePreviewLinePosition()
    {
        Vector2 mousePos = GetLocalMousePosition();
        float musicPosition = XPositionToMusicPosition(mousePos.X);

        if (Input.IsKeyPressed(Key.Shift))
        {
            musicPosition = Timing.Instance.SnapMusicPosition(musicPosition);
        }

        PreviewLine.Position = new Vector2(MusicPositionToXPosition(musicPosition), 0);

        float time = Timing.Instance.MusicPositionToTime(musicPosition);
        var musicTime = TimeSpan.FromSeconds(time);
        PreviewLine.TimeLabel.Text = (time < 0 ? "-" : "") + musicTime.ToString(@"mm\:ss\:fff");
    }

    public void UpdateSelectedPositionLine()
    {
        float x = MusicPositionToXPosition(Context.Instance.SelectedMusicPosition);
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

        float margin = Settings.Instance.MusicPositionMargin;

        int firstIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MusicPosition <= ActualMusicPositionStartForPanel);
        // If there's only TimingPoints AFTER MusicPositionStart
        if (firstIndex == -1)
            firstIndex = Timing.Instance.TimingPoints.FindIndex(point => point.MusicPosition > ActualMusicPositionStartForPanel);
        int lastIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MusicPosition < ActualMusicPositionStartForPanel + 1 + (2 * margin));
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
        float xAtSelectorStart = MusicPositionToXPosition((float)TimingPointSelection.Instance.SelectorStartPosition);
        float xAtSelectorEnd = MusicPositionToXPosition((float)TimingPointSelection.Instance.SelectorEndPosition);
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

    public float XPositionToMusicPosition(float x) =>
        //GD.Print($"XPositionToMusicPosition: NominalMusicPositionStartForWindow = {NominalMusicPositionStartForWindow}");
        XPositionToRelativeMusicPosition(x) + NominalMusicPositionStartForWindow;

    /// <summary>
    ///     Return music position relative to <see cref="NominalMusicPositionStartForWindow" />
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public float XPositionToRelativeMusicPosition(float x)
    {
        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + (2 * margin);
        return (x * windowLengthInMeasures / Size.X) - margin - offset;
    }

    public float MusicPositionToXPosition(float musicPosition)
    {
        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + (2 * margin);
        return Size.X * (musicPosition - NominalMusicPositionStartForWindow + margin + offset) / windowLengthInMeasures;
    }

    #endregion
}