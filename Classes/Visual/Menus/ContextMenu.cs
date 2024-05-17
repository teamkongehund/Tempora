using Godot;
using System;
using System.Collections.Generic;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;
using Tempora.Classes.Visual;

/// <summary>
/// A menu to display options, i.e. for a timing point
/// </summary>
public partial class ContextMenu : PopupMenu
{
    /// <summary>
    /// The object that the user will get a context menu for. 
    /// </summary>
    public object? TargetObject;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        GlobalEvents.Instance.ContextMenuRequested += OnContextMenuRequested;
        IdPressed += OnIdPressed;
	}

    private void OnContextMenuRequested(object? sender, EventArgs eventArgs)
    {
        if (eventArgs == EventArgs.Empty)
            return;

        var visualTimingPointArgument = (GlobalEvents.ObjectArgument<VisualTimingPoint>)eventArgs;

        if (visualTimingPointArgument == null)
            return;

        TargetObject = visualTimingPointArgument.Value;

        ShowMenu();
    }

	public void ShowMenu()
    {

        bool shouldShowMenu = GenerateOptions();
        if (!shouldShowMenu)
        {
            Visible = false;
            return;
        }

        Vector2 mousePosition = GetTree().Root.GetMousePosition();
        Position = (Vector2I)mousePosition;

        Visible = true;
    }

    /// <summary>
    /// Fills the menu with items based on the <see cref="TargetObject"/>
    /// </summary>
    /// <returns>Whether any items were created</returns>
    private bool GenerateOptions()
    {
        Clear();

        switch (TargetObject)
        {
            case VisualTimingPoint visualTimingPoint:
                GenerateOptionsForVisualTimingPoint(visualTimingPoint);
                break;
            default:
                return false;
        }

        return true;
    }

    private void GenerateOptionsForVisualTimingPoint(VisualTimingPoint visualTimingPoint)
    {
        AddItem("Double BPM", OptionIDs[Options.DoubleBPM]);
        AddItem("Halve BPM", OptionIDs[Options.HalveBPM]);

        var timingPoint = visualTimingPoint.TimingPoint;

        var selection = TimingPointSelection.Instance;
        bool isInSelection = selection.IsPointInSelection(timingPoint);
        if (selection.Count > 1 && isInSelection)
            AddItem("Delete Timing Points", OptionIDs[Options.DeleteTimingPoints]);
        else
            AddItem("Delete Timing Point", OptionIDs[Options.DeleteTimingPoint]);

        if (!isInSelection && selection.AreTherePointsBefore(timingPoint))
            AddItem("Select all points from beginning to here", OptionIDs[Options.SelectAllFromBeginningToHere]);
        else if (selection.SelectionIndices != null && isInSelection && selection.AreTherePointsBefore(selection.SelectionIndices[0]))  
            AddItem("Extend selection to the beginning", OptionIDs[Options.ExtendSelectionToBeginning]);

        if (!isInSelection && selection.AreTherePointsAfter(timingPoint))
            AddItem("Select all points from here to the end", OptionIDs[Options.SelectAllFromHereToEnd]);
        else if (selection.SelectionIndices != null && isInSelection && selection.AreTherePointsAfter(selection.SelectionIndices[1]))
            AddItem("Extend selection to the end", OptionIDs[Options.ExtendSelectionToTheEnd]);

    }

    private enum Options
    {
        DoubleBPM,
        HalveBPM,
        DeleteTimingPoint,
        DeleteTimingPoints,
        SelectAllFromBeginningToHere,
        SelectAllFromHereToEnd,
        ExtendSelectionToBeginning,
        ExtendSelectionToTheEnd
    }

    private Dictionary<Options, int> OptionIDs = new Dictionary<Options, int>()
    {
        { Options.DoubleBPM, 0 },
        { Options.HalveBPM, 1 },
        { Options.DeleteTimingPoint, 2 },
        { Options.DeleteTimingPoints, 3 },
        { Options.SelectAllFromBeginningToHere, 5 },
        { Options.SelectAllFromHereToEnd, 4 },
        { Options.ExtendSelectionToBeginning, 6 },
        { Options.ExtendSelectionToTheEnd, 7 },
    };

    private void OnIdPressed(long idLong)
    {
        int id = (int)idLong;
        ActivateOption(id);
    }

    private void ActivateOption(int id)
    {
        VisualTimingPoint? visualTimingPoint = (VisualTimingPoint?)TargetObject;
        TimingPoint? timingPoint = visualTimingPoint?.TimingPoint;

        void assertTargetIsTimingPoint()
        {
            if (TargetObject is not VisualTimingPoint)
                throw new Exception("Menu item requires TargetObject to be VisualTimingPoint");
        }

        var selection = TimingPointSelection.Instance;

        switch (id)
        {
            case var value when id == OptionIDs[Options.DoubleBPM]:
                assertTargetIsTimingPoint();
                Timing.Instance.ScaleTempo(timingPoint, 2);
                break;
            case var value when id == OptionIDs[Options.HalveBPM]:
                assertTargetIsTimingPoint();
                Timing.Instance.ScaleTempo(timingPoint, 0.5f);
                break;
            case var value when (id == OptionIDs[Options.DeleteTimingPoint] || id == OptionIDs[Options.DeleteTimingPoints]):
                assertTargetIsTimingPoint();
                Timing.Instance.DeleteTimingPointOrSelection(timingPoint);
                break;
            case var value when (id == OptionIDs[Options.SelectAllFromBeginningToHere]):
                assertTargetIsTimingPoint();
                selection.SelectAllTo(timingPoint);
                break;
            case var value when (id == OptionIDs[Options.SelectAllFromHereToEnd]):
                assertTargetIsTimingPoint();
                selection.SelectAllFrom(timingPoint);
                break;
            case var value when (id == OptionIDs[Options.ExtendSelectionToBeginning]):
                assertTargetIsTimingPoint();
                selection.SelectAllTo(selection?.SelectionIndices?[1]);
                break;
            case var value when (id == OptionIDs[Options.ExtendSelectionToTheEnd]):
                assertTargetIsTimingPoint();
                selection.SelectAllFrom(selection?.SelectionIndices?[0]);
                break;
        }
        TargetObject = null; // This assumes only one item is selectable at a time
    }
}
