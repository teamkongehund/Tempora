using Godot;
using System;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

public partial class TimingPointSelection : Node
{
    public event EventHandler? SelectorChanged = null;
    public event EventHandler? SelectionChanged = null;

    private Timing timing => Timing.Instance;

    public static TimingPointSelection Instance = null!;

    public override void _Ready()
    {
        Instance = this;

        GlobalEvents.Instance.MouseLeftReleased += OnMouseLeftReleased;

        GlobalEvents.Instance.TimingPointCountChanged += OnTimingPointCountChanged;
    }
    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            //case InputEventMouseButton mouseEvent:
            //    if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed && Input.IsKeyPressed(Key.Alt))
            //        TimingPointSelection.Instance.DeselectAll();
            //    break;
            case InputEventKey keyEvent:
                if (keyEvent.Keycode == Key.A && keyEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
                    Select(0, timing.TimingPoints.Count - 1);
                else if (keyEvent.Keycode == Key.Delete || keyEvent.Keycode == Key.Backspace)
                    DeleteSelection();
                break;
        }
    }

    #region Selector
    public float? SelectorStaticPosition = null;

    public float? SelectorDynamicPosition = null;

    public float? SelectorStartPosition
    {
        get
        {
            if (SelectorStaticPosition == null || SelectorDynamicPosition == null)
                return null;
            return Math.Min((float)SelectorStaticPosition, (float)SelectorDynamicPosition);
        }
    }

    public float? SelectorEndPosition
    {
        get
        {
            if (SelectorStaticPosition == null || SelectorDynamicPosition == null)
                return null;
            return Math.Max((float)SelectorStaticPosition, (float)SelectorDynamicPosition);
        }
    }

    public int Count
    {
        get
        {
            if (SelectionIndices == null) return 0;
            else return SelectionIndices[1] - SelectionIndices[0] + 1;
        }
    }

    public void StartSelector(float position)
    {
        if (SelectionIndices == null)
        {
            SelectorStaticPosition = position;
            SelectorDynamicPosition = position;
            return;
        }
        SelectorStaticPosition = position > CenterPosition ? firstPosition : lastPostion;
        SelectorDynamicPosition = position;
        ApplySelector();
    }

    public void UpdateSelector(float position)
    {
        if (SelectorStartPosition == null || SelectorEndPosition == null)
            return;
        SelectorDynamicPosition = position;
        ApplySelector();
    }

    private void ApplySelector()
    {
        if (SelectorStartPosition == null || SelectorEndPosition == null)
            return;
        Select((float)SelectorStartPosition, (float)SelectorEndPosition);
        SelectorChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StopSelector()
    {
        SelectorStaticPosition = null;
        SelectorDynamicPosition = null;
        SelectorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnMouseLeftReleased(object? sender, EventArgs e)
    {
        ApplySelector();
        StopSelector();
        MementoHandler.Instance.AddSelectionMemento();
    }
    #endregion

    #region Selection Properties
    private int[]? selectionIndices = null;
    /// <summary>
    /// The beginning and end indices of <see cref="Timing.TimingPoints"/> that are a part of this selection.
    /// Both indices are included
    /// </summary>
    public int[]? SelectionIndices
    {
        get => selectionIndices;
        private set
        {
            if (value == selectionIndices)
                return;

            bool wasNull = (selectionIndices == null || selectionIndices[0] == -1 || selectionIndices[1] == -1);

            if (value == null || value[0] == -1 || value[1] == -1)
            {
                selectionIndices = null;
                if (wasNull) return;
            }
            else
                selectionIndices = value;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public TimingPoint? FirstPoint
    {
        get
        {
            if (SelectionIndices == null)
                return null;
            return timing.TimingPoints[SelectionIndices[0]];
        }
    }
    public TimingPoint? LastPoint
    {
        get
        {
            if (SelectionIndices == null)
                return null;
            return timing.TimingPoints[SelectionIndices[1]];
        }
    }

    private float? firstPosition => FirstPoint?.MusicPosition;
    private float? lastPostion => LastPoint?.MusicPosition;

    public float? CenterPosition
    {
        get
        {
            if (SelectionIndices == null || FirstPoint == null || LastPoint == null)
                return null;
            return (LastPoint.MusicPosition - FirstPoint.MusicPosition) / 2 + FirstPoint.MusicPosition;
        }
    }
    #endregion

    #region Selection Modifiers
    private void Select(int first, int last)
    {
        if (first > last)
            (first, last) = (last, first);
        SelectionIndices = [first, last];
    }

    private void Select(float positionFirst, float positionLast)
    {
        if (positionFirst > positionLast)
            (positionFirst, positionLast) = (positionLast, positionFirst);
        int firstIndex = timing.TimingPoints.FindIndex(point => point.MusicPosition >= positionFirst && point.MusicPosition <= positionLast);
        int lastIndex = timing.TimingPoints.FindLastIndex(point => point.MusicPosition >= positionFirst && point.MusicPosition <= positionLast);
        Select(firstIndex, lastIndex);
    }
    public void DeselectAll() => SelectionIndices = null;

    /// <summary>
    /// If point is outside selection, select the point exclusively. Otherwise, do nothing.
    /// </summary>
    /// <param name="point"></param>
    public void SelectTimingPoint(TimingPoint point)
    {
        if (IsPointInSelection(point))
            return;
        int index = timing.TimingPoints.IndexOf(point);
        Select(index, index);
    }

    /// <summary>
    /// Change selection such that this point is either just included or just excluded at the edge of the selection.
    /// If it's the only point in selection, deselect it.
    /// </summary>
    /// <param name="point"></param>
    public void RescopeSelection(TimingPoint point)
    {
        int index = timing.TimingPoints.IndexOf(point);

        bool isPointInSelection = IsPointInSelection(point);

        if (SelectionIndices == null)
        {
            SelectTimingPoint(point);
            return;
        }
        if (Count == 1 && isPointInSelection)
        {
            DeselectAll();
            return;
        }

        bool isPointBeforeSelectionCenter = point.MusicPosition < CenterPosition;
        
        SelectionIndices = isPointBeforeSelectionCenter
            ? [isPointInSelection ? index + 1 : index, SelectionIndices[1]]
            : [SelectionIndices[0], isPointInSelection ? index - 1 : index];
    }

    public void DeleteSelection()
    {
        if (SelectionIndices == null)
            return;
        timing.DeleteTimingPoints(SelectionIndices[0], SelectionIndices[1] + 1);
        SelectionIndices = null;
    }

    public void MoveSelection(float? referencePosition, float newPosition)
    {
        if (SelectionIndices == null || referencePosition == null) return;

        newPosition = Timing.Instance.SnapMusicPosition(newPosition);

        float positionDifference = newPosition - (float)referencePosition;

        float getNewPosition(TimingPoint? point) => (point?.MusicPosition ?? 0) + positionDifference;

        timing.BatchChangeMusicPosition(SelectionIndices[0], SelectionIndices[1], getNewPosition);
    }

    /// <summary>
    /// If the point is in selection, offset the entire selection. Otherwise, offset the point.
    /// </summary>
    public void OffsetSelectionOrPoint(TimingPoint? point, float offset)
    {
        if (point == null) return;
        if (IsPointInSelection(point))
            OffsetSelection(offset);
        else
        {
            point.Offset_Set(point.Offset + offset, Timing.Instance);
            MementoHandler.Instance.AddTimingMemento(point);
        }
    }

    public void OffsetSelection(float offset)
    {
        if (SelectionIndices == null) return;
        timing.BatchChangeOffset(SelectionIndices[0], SelectionIndices[1], offset);
        MementoHandler.Instance.AddTimingMemento(SelectionIndices);
    }

    public void DoubleTempoForSelection()
    {
        if (SelectionIndices == null) return;
        Timing.Instance.ScaleTempo(SelectionIndices[0], SelectionIndices[1], 2);
        //MementoHandler.Instance.AddTimingMemento(SelectionIndices);
    }

    public void HalveTempoForSelection()
    {
        if (SelectionIndices == null) return;
        Timing.Instance.ScaleTempo(SelectionIndices[0], SelectionIndices[1], 0.5f);
        //MementoHandler.Instance.AddTimingMemento(SelectionIndices);
    }

    private void OnTimingPointCountChanged(object? sender, EventArgs e)
    {
        DeselectAll();
    }
    #endregion

    public bool IsPointInSelection(TimingPoint point)
    {
        int index = timing.TimingPoints.IndexOf(point);
        if (index == -1) return false;
        if (SelectionIndices == null) return false;
        return (index >= SelectionIndices[0] && index <= SelectionIndices[1]);
    }

    /// <summary>
    /// Description of a selection of multiple timing points:
    /// You can only select timing points in a continous series (no unselected points between selected points)
    /// You hold alt and left click drag to select them
    /// 
    /// SelectorBar:
    /// A colored fill bar that responds to alt-left-click-dragging: it will add/remove timing points to selection based on context
    /// 
    /// SelectionBar:
    /// A colored corner bar, which has horizontal lines in the top and bottom of an AudioDisplayPanel, 
    /// spanning across all selected timing points. 
    /// It stops on either end of the selection with two vertical line through the first and last timing point in selection
    /// 
    /// SelectionChanged event
    /// - This connects to all VisualTimingPoints and updates their looks based on whether they are selected or not.
    /// - Updates many times as the user is gradually changing his selection
    /// 
    /// SelectionMemento:
    /// On LeftClickReleased, checks if the selection has changed. 
    /// If it has, a Memento is added to capture this selection.
    /// 
    /// WHEN WE DON'T HAVE A SELECTION:
    /// 
    /// Click on Timing Point: select it.
    /// 
    /// Alt-left-click-drag: Make SelectorBar appear. If there's only a single timing point in selection, deselect it.
    /// User can now create selection by dragging across timing points.
    /// 
    /// WHEN WE HAVE A SELECTION:
    /// 
    /// Releasing Alt as SelectorBar is visible: nothing happens
    /// 
    /// Releasing left click when SelectorBar is visible: SelectorBar becomes Invisible and we're no longer modifying selection.
    /// 
    /// Clicking a timing point in selection: Nothing happens
    /// 
    /// Clicking a timing point outside of selection: Deselect all and select this point instead
    /// 
    /// Left-click-dragging anywhere in selection: change musicposition of all timing points in selection
    ///
    /// Alt-left-clicking in selection: based on the middle music position of selection, 
    /// make the SelectorBar visible and reduce selection to this music position
    /// - if there's only a single point selected and we're alt-clicking on it, deselct it
    /// 
    /// Double-clicking a timing point in selection: Delete all selected timing points, then deselect all
    /// 
    /// Alt-double-click anywhere: Deselct all
    /// 
    /// Alt-right-click anywhere in selection: context menu for selected timing points
    /// 
    /// Alt-right-click on timeline outside selection: Deselect all
    /// 
    /// Alt-right-click on timing point outside selection: Deselect all and open context menu for point.
    /// 
    /// </summary>
    bool asdf;
}
