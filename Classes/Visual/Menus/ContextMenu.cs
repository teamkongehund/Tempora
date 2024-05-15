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

        var selection = TimingPointSelection.Instance;
        if (selection.Count > 1 && selection.IsPointInSelection(visualTimingPoint.TimingPoint))
            AddItem("Delete Timing Points", OptionIDs[Options.DeleteTimingPoints]);
        else
            AddItem("Delete Timing Point", OptionIDs[Options.DeleteTimingPoint]);

    }

    private enum Options
    {
        DoubleBPM,
        HalveBPM,
        DeleteTimingPoint,
        DeleteTimingPoints
    }

    private Dictionary<Options, int> OptionIDs = new Dictionary<Options, int>()
    {
        { Options.DoubleBPM, 0 },
        { Options.HalveBPM, 1 },
        { Options.DeleteTimingPoint, 2 },
        { Options.DeleteTimingPoints, 3 },
    };

    private void OnIdPressed(long idLong)
    {
        int id = (int)idLong;
        ActivateOption(id);
    }

    private void ActivateOption(int id)
    {
        VisualTimingPoint? visualTimingPoint = (VisualTimingPoint?)TargetObject;
        switch (id)
        {
            case var value when id == OptionIDs[Options.DoubleBPM]:
                if (TargetObject is not VisualTimingPoint)
                    throw new Exception("Menu item requires TargetObject to be VisualTimingPoint");
                Timing.Instance.ScaleTempo(visualTimingPoint?.TimingPoint, 2);
                break;
            case var value when id == OptionIDs[Options.HalveBPM]:
                if (TargetObject is not VisualTimingPoint)
                    throw new Exception("Menu item requires TargetObject to be VisualTimingPoint");
                Timing.Instance.ScaleTempo(visualTimingPoint?.TimingPoint, 0.5f);
                break;
            case var value when (id == OptionIDs[Options.DeleteTimingPoint] || id == OptionIDs[Options.DeleteTimingPoints]):
                if (TargetObject is not VisualTimingPoint)
                    throw new Exception("Menu item requires TargetObject to be VisualTimingPoint");
                Timing.Instance.DeleteTimingPointOrSelection(visualTimingPoint?.TimingPoint);
                break;
        }
        TargetObject = null; // This assumes only one item is selectable at a time
    }
}
