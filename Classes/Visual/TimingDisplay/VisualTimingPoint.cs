using System;
using System.Collections.Generic;
using Godot;
using GD = Tempora.Classes.DataHelpers.GD;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
//using System.Drawing;

namespace Tempora.Classes.Visual;

public partial class VisualTimingPoint : Node2D
{
    #region Properties & Fields
    [Export]
    private Area2D area2D = null!;
    [Export]
    public Label BpmLabel = null!;
    [Export]
    private CollisionShape2D collisionShape2D = null!;
    //[Export]
    //private Label numberLabel = null!;
    [Export]
    private ColorRect colorRect = null!;
    [Export]
    public Line2D OffsetLine = null!;
    [Export]
    private Timer flashTimer = null!;
    private bool isFlashActive => !flashTimer.IsStopped();
    private bool isRed = false;
    private Vector2 sizeDefault = new(128, 128);
    private Vector2 rectSizeRed => sizeDefault * 1.5f;
    private Vector2 rectSizeNearestCursor => sizeDefault * 1.3f;
    private Color rectColorDefault = new("ff990096");
    private Color rectColorLightup = new("ff990096");
    private Color rectColorNearestCursor = new("ff990096");
    private Color rectColorSelection = new("ab009196");
    private Color rectColorRed = new("ff000096");

    private Color colorInvisible = new("ff990000");

    private Color lineColorRed = new("ff0000");
    private Color lineColorDefault = new("ff9900");
    private Color lineColorSelection = new("ab0091");
    //private Color lineColorNearestCursor = new("00d49c");
    private Color lineColorNearestCursor = new("ff9900");

    public float LineDefaultHeight = 0;
    private float lineDefaultWidth = 5;

    private float lineSizeRed = 2;
    private float lineSizeNearedCursor = 1.3f;
    private float lineSizeDefault = 1;

    private bool isNearestCursor = false;
    private bool isSelected => TimingPointSelection.Instance.IsPointInSelection(TimingPoint);

    private TimingPoint timingPoint = null!;
    public TimingPoint TimingPoint
    {
        get => timingPoint;
        set
        {
            if (timingPoint == value)
                return;
            if (timingPoint != null)
                timingPoint.Changed -= OnTimingPointChanged;
            timingPoint = value;
            TimingPoint.Changed += OnTimingPointChanged;
            //SubscribeToTimingPointEvents();
        }
    } 
    #endregion

    #region Godot
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //numberLabel.Text = Timing.Instance.TimingPoints.IndexOf(TimingPoint).ToString();
        BpmLabel.Text = TimingPoint.Bpm.ToString("0.00");

        //SystemTimeWhenCreated = Time.GetTicksMsec();

        //SubscribeToTimingPointEvents();

        //VisibilityChanged += OnVisibilityChanged;

        GlobalEvents.Instance.MusicPositionChangeRejected += OnMusicPositionChangeRejected;
        GlobalEvents.Instance.TimingPointNearestCursorChanged += OnTimingPointNearestCursorChanged;
        TimingPointSelection.Instance.SelectionChanged += OnSelectionChanged;

        VisibilityChanged += OnVisibilityChanged;

        flashTimer.Timeout += OnFlashTimerTimeout;
        rectColorDefault = colorRect.Color;
        lineColorDefault = OffsetLine.DefaultColor;
        sizeDefault = colorRect.Size;

        lineDefaultWidth = OffsetLine.Width;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;
        Vector2 mousePosition = GetLocalMousePosition();
        Rect2 rectangle = collisionShape2D.Shape.GetRect();
        bool hasMouseInside = rectangle.HasPoint(mousePosition);

        if (@event is not InputEventMouseButton mouseEvent)
            return;
        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
        {
            //Signals.Instance.EmitEvent(Signals.Events.MouseLeftReleased);
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.MouseLeftReleased), this, EventArgs.Empty);
            return;
        }
        if (!hasMouseInside)
            return;

        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && Input.IsKeyPressed(Key.Alt))
        {
            TimingPointSelection.Instance.DeselectAll();
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick)
        {
            TimingPointSelection.Instance.DeleteSelection();
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && Input.IsKeyPressed(Key.Alt))
        {
            TimingPointSelection.Instance.RescopeSelection(TimingPoint);
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            TimingPointSelection.Instance.SelectTimingPoint(TimingPoint);
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointHolding), new GlobalEvents.ObjectArgument<TimingPoint>(TimingPoint));
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed && Input.IsKeyPressed(Key.Alt))
        {
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.ContextMenuRequested), new GlobalEvents.ObjectArgument<VisualTimingPoint>(this));
        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
        {
            // Decrease BPM by 1 (snapping to integers) - only for last timing point.
            float previousBpm = TimingPoint.Bpm;
            float newBpm = (int)previousBpm - 1;
            if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                newBpm = (int)previousBpm - 5;
            else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                newBpm = previousBpm - 0.1f;

            TimingPoint.Bpm_Set(newBpm, Timing.Instance);

            MementoHandler.Instance.AddTimingMemento(TimingPoint);
        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
        {
            // Increase BPM by 1 (snapping to integers) - only for last timing point.
            float previousBpm = TimingPoint.Bpm;
            float newBpm = (int)previousBpm + 1;
            if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                newBpm = (int)previousBpm + 5;
            else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                newBpm = previousBpm + 0.1f;

            TimingPoint.Bpm_Set(newBpm, Timing.Instance);

            MementoHandler.Instance.AddTimingMemento(TimingPoint);
        }
        else
            return;

        GetViewport().SetInputAsHandled();
    }
    #endregion

    #region Timing Changes
    public void UpdateLabels(TimingPoint timingPoint)
    {
        //numberLabel.Text = Timing.Instance.TimingPoints.IndexOf(timingPoint).ToString();
        BpmLabel.Text = timingPoint.Bpm.ToString("0.00");
    }

    private void DeleteTimingPoint()
    {
        GD.Print("DeleteTimingPoint() started");

        // Prevent accidental deletion un inadvertent double-double-clicking. Instead treated as holding the timing point
        if (Time.GetTicksMsec() - TimingPoint.SystemTimeWhenCreatedMsec <= 500)
        {
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointHolding), new GlobalEvents.ObjectArgument<TimingPoint>(TimingPoint));
            return;
        }
        else
        {
            TimingPoint.Delete();
        }

        Viewport viewport = GetViewport();
        viewport.SetInputAsHandled();
        return;
    } 
    #endregion

    #region Events

    private void OnTimingPointChanged(object? sender, EventArgs e)
    {
        if (!Visible) return;
        if (sender is not TimingPoint timingPoint) return;
        UpdateLabels(timingPoint);
    }

    private void OnFlashTimerTimeout()
    {
        if (isRed)
        {
            isRed = false;
            flashTimer.Start(); // period of regular looks before it can flash flash again
            UpdateLooks();
        }
    }

    private void OnMusicPositionChangeRejected(object? sender, EventArgs e)
    {
        if (!Visible) 
            return;
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            return;
        if (timingPointArgument.Value != TimingPoint)
            return;
        FlashRed();
    }

    private void OnTimingPointNearestCursorChanged(object? sender, EventArgs e)
    {
        if (!Visible) return;
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            throw new Exception($"{nameof(GlobalEvents.TimingPointNearestCursorChanged)} was invoked without a {nameof(GlobalEvents.ObjectArgument<TimingPoint>)}");
        isNearestCursor = timingPointArgument.Value == TimingPoint;
        UpdateLooks();
    }

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateLooks();

    private void OnVisibilityChanged() => UpdateLooks();
    #endregion

    #region Change Looks
    private void FlashRed()
    {
        //GD.Print($"VisualTimingPoint with {TimingPoint.MusicPosition}: Flashing Red!");
        if (isFlashActive) return;
        flashTimer.Start();
        isRed = true;
        UpdateLooks();
    }

    private void UpdateLooks()
    {
        if (!Visible) return;
        //SetRectColor(isRed ? colorRed : isSelected ? rectColorSelection : isNearestCursor ? rectColorNearestCursor : colorInvisible);
        //SetRectSize(isRed ? rectSizeRed : isNearestCursor ? rectSizeNearestCursor : sizeDefault);
        SetRectColor(colorInvisible);
        SetLineColor(isRed ? lineColorRed : isSelected ? lineColorSelection : isNearestCursor ? lineColorNearestCursor : lineColorDefault);
        SetLineSize(isRed ? lineSizeRed : isNearestCursor ? lineSizeNearedCursor : lineSizeDefault);
    }

    private void SetRectSize(Vector2 size)
    {
        colorRect.Size = size;
        colorRect.Position = -size / 2;
        colorRect.PivotOffset = size / 2;
    }

    private void SetLineSize(float multiplier)
    {
        if (OffsetLine.Points.Length == 0)
            return;
        OffsetLine.Points = [
                new(0, -LineDefaultHeight / 2 * multiplier),
                new(0, LineDefaultHeight / 2 * multiplier)
            ];
        OffsetLine.Width = lineDefaultWidth * multiplier;
    }

    private void SetRectColor(Color color)
    {
        if (colorRect.Color == color) return;
        colorRect.Color = color;
    } 

    private void SetLineColor(Color color)
    {
        if (OffsetLine.DefaultColor == color) return;
        OffsetLine.DefaultColor = color;
    }
    #endregion
}