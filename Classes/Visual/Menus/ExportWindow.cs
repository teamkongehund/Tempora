using Godot;
using System;
using System.Runtime.CompilerServices;
using Tempora.Classes.Utility;

public partial class ExportWindow : Window
{
    [Export]
    LineEdit exportOffsetEdit = null!;

    [Export]
    CheckBox unsupportedTimeSignatures = null!;

    [Export]
    CheckBox removePointsThatChangeNothing = null!;

    [Export]
    CheckBox addExtraPointsOnDownbeats= null!;

    [Export]
    CheckBox addExtraPointsOnQuarterNotes= null!;

    [Export]
    CheckBox omitBarlines = null!;

    [Export]
    CheckBox preventDoubleBarlines = null!;

    [Export]
    Button okButton = null!;

    [Export]
    Button defaultsButton = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        okButton.Pressed += OnOkButtonPressed;
        defaultsButton.Pressed += OnResetButtonPressed;

        CloseRequested += CloseWithoutSaving;
        AboutToPopup += OnAboutToPopup;
	}

    private void LoadValuesFromSettings()
    {
        exportOffsetEdit.Text = Settings.Instance.ExportOffsetMs.ToString();
        unsupportedTimeSignatures.ButtonPressed = Settings.Instance.MeasureResetsOnUnsupportedTimeSignatures;
        removePointsThatChangeNothing.ButtonPressed = Settings.Instance.RemovePointsThatChangeNothing;
        addExtraPointsOnDownbeats.ButtonPressed = Settings.Instance.AddExtraPointsOnDownbeats;
        addExtraPointsOnQuarterNotes.ButtonPressed = Settings.Instance.AddExtraPointsOnQuarterNotes;
        omitBarlines.ButtonPressed = Settings.Instance.OmitBarlines;
        preventDoubleBarlines.ButtonPressed = Settings.Instance.PreventDoubleBarlines;
    }

    private void OnAboutToPopup()
    {
        LoadValuesFromSettings();
    }

    private void OnOkButtonPressed()
    {
        SaveSettings();
        Hide();
        Settings.Instance.SaveSettings();
        Project.Instance.NotificationMessage = "Saved export options.";
    }

    private void OnResetButtonPressed()
    {
        ResetToDefaults();
    }

    private void SaveSettings()
    {
        bool exportOffsetParsed = int.TryParse(exportOffsetEdit.Text, out int exportOffset);
        if (exportOffsetParsed)
        {
            Settings.Instance.ExportOffsetMs = exportOffset;
        }
        Settings.Instance.MeasureResetsOnUnsupportedTimeSignatures = unsupportedTimeSignatures.ButtonPressed;
        Settings.Instance.RemovePointsThatChangeNothing = removePointsThatChangeNothing.ButtonPressed;
        Settings.Instance.AddExtraPointsOnDownbeats = addExtraPointsOnDownbeats.ButtonPressed;
        Settings.Instance.AddExtraPointsOnQuarterNotes = addExtraPointsOnQuarterNotes.ButtonPressed;
        Settings.Instance.OmitBarlines = omitBarlines.ButtonPressed;
        Settings.Instance.PreventDoubleBarlines = preventDoubleBarlines.ButtonPressed;
    }

    private void ResetToDefaults()
    {
        exportOffsetEdit.Text = (-29).ToString();
        unsupportedTimeSignatures.ButtonPressed = true;
        removePointsThatChangeNothing.ButtonPressed = true;
        addExtraPointsOnDownbeats.ButtonPressed = true;
        addExtraPointsOnQuarterNotes.ButtonPressed = true;
        omitBarlines.ButtonPressed = true;
        preventDoubleBarlines.ButtonPressed = true;
    }

    private void CloseWithoutSaving()
    {
        Hide();
        Project.Instance.NotificationMessage = "Export options were not saved.";
    }
}
