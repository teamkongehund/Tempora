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
		SetItemChecked(id_PreserveBpm, Settings.Instance.PreserveBPMWhenChangingTimeSignature);
		SetItemChecked(id_MetronopmeFollowsGrid, Settings.Instance.MetronomeFollowsGrid);
		SetItemChecked(id_AutoScroll, Settings.Instance.AutoScrollWhenAddingTimingPoints);
		SetItemChecked(id_RoundBPM, Settings.Instance.RoundBPM);
        SetItemChecked(id_PlaybackOnNewPoints, Settings.Instance.SeekPlaybackOnTimingPointChanges);
        SetItemChecked(id_MoreSettings, Settings.Instance.ShowMoreSettings);
	}

	int id_PreserveBpm = 0;
	int id_MetronopmeFollowsGrid = 1;
	int id_AutoScroll = 2;
	int id_RoundBPM = 3;
	int id_MoreSettings = 4;
    int id_PlaybackOnNewPoints = 5;

	private void OnIdPressed(long id)
	{
		switch (id)
		{
			case var expression when (id == id_PreserveBpm):
				ToggleCheckBox(id_PreserveBpm, out bool newStatus);
				Settings.Instance.PreserveBPMWhenChangingTimeSignature = newStatus;
				break;
			case var expression when (id == id_MetronopmeFollowsGrid):
				ToggleCheckBox(id_MetronopmeFollowsGrid, out newStatus);
				Settings.Instance.MetronomeFollowsGrid = newStatus;
				break;
			case var expression when (id == id_MoreSettings):
				ToggleCheckBox(id_MoreSettings, out newStatus);
				ShowHideMoreSettings(newStatus);
				break;
			case var expression when (id == id_AutoScroll):
				ToggleCheckBox(id_AutoScroll, out newStatus);
				Settings.Instance.AutoScrollWhenAddingTimingPoints = newStatus;
				break;
			case var expression when (id == id_RoundBPM):
				ToggleCheckBox(id_RoundBPM, out newStatus);
				Settings.Instance.RoundBPM = newStatus;
				break;
            case var expression when (id == id_PlaybackOnNewPoints):
                ToggleCheckBox(id_PlaybackOnNewPoints, out newStatus);
                Settings.Instance.SeekPlaybackOnTimingPointChanges = newStatus;
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
