using Godot;
using System;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class OptionsMenu : PopupMenu
{
    [Export]
    private Control blockAmountScrollBar = null!;
    [Export]
    private Control offsetScrollBar = null!;
    [Export]
    private Control overlapScrollBar = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        IdPressed += OnIdPressed;
        SetItemChecked(0, Settings.Instance.MoveSubsequentTimingPointsWhenChangingTimeSignature);
        SetItemChecked(1, Settings.Instance.MetronomeFollowsGrid);
    }

    int id_PreserveBpm = 0;
    int id_MetronopmeFollowsGrid = 1;
    int id_MoreSettings = 2;

    private void OnIdPressed(long id)
    {
        switch (id)
        {
            case var expression when (id == id_PreserveBpm):
                ToggleCheckBox(id_PreserveBpm, out bool newStatus);
                Settings.Instance.MoveSubsequentTimingPointsWhenChangingTimeSignature = newStatus;
                break;
            case var expression when (id == id_MetronopmeFollowsGrid):
                ToggleCheckBox(id_MetronopmeFollowsGrid, out newStatus);
                Settings.Instance.MetronomeFollowsGrid = newStatus;
                break;
            case var expression when (id == id_MoreSettings):
                ToggleCheckBox(id_MoreSettings, out newStatus);
                ShowHideMoreSettings(newStatus);
                break;
        }
    }

    private void ToggleCheckBox(int id, out bool newStatus)
    {
        int index = GetItemIndex(id);
        bool isChecked = IsItemChecked(index);
        SetItemChecked(index, !isChecked);
        newStatus = IsItemChecked(index);
    }

    private void ShowHideMoreSettings(bool visible)
    {
        blockAmountScrollBar.Visible = visible;
        offsetScrollBar.Visible = visible;
        overlapScrollBar.Visible = visible;
    }
}